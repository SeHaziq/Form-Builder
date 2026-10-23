import { useEffect, useState, type SyntheticEvent } from "react";

import { Link, useParams } from "react-router";
import type { FormSummary, FormQuestion } from "../../api/forms";
import {
  createQuestion,
  getForm,
  getQuestions,
  updateQuestion,
  deleteQuestion,
  reorderQuestions,
  publishForm,
  createDraft,
  getPublishedQuestions,
} from "../../api/forms";

export default function FormEditorPage() {
  const { id } = useParams<{ id: string }>();
  const [isEditing, setIsEditing] = useState(false);

  const [form, setForm] = useState<FormSummary | null>(null);
  const [questions, setQuestions] = useState<FormQuestion[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [questionLabel, setQuestionLabel] = useState("");
  const [questionType, setQuestionType] = useState(0);
  const [questionRequired, setQuestionRequired] = useState(false);
  const [questionOptions, setQuestionOptions] = useState("");
  const [savingQuestion, setSavingQuestion] = useState(false);
  const [editingQuestionId, setEditingQuestionId] = useState<string | null>(
    null,
  );
  const [publishing, setPublishing] = useState(false);
  const [publishedVersion, setPublishedVersion] = useState<number | null>(null);

  async function handlePublish() {
    if (!id) return;

    setError("");
    setPublishing(true);

    try {
      const result = await publishForm(id);
      setPublishedVersion(result.versionNumber);
      const publishedQuestions = await getPublishedQuestions(id);
setQuestions(publishedQuestions);
setIsEditing(false);
setEditingQuestionId(null);
setQuestionLabel("");
setQuestionType(0);
setQuestionRequired(false);
setQuestionOptions("");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to publish the form.",
      );
    } finally {
      setPublishing(false);
    }
  }

  async function handleCreateQuestion(event: SyntheticEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!id) {
      setError("Form ID is missing.");
      return;
    }

    const label = questionLabel.trim();
    if (!label) {
      setError("Please enter a question.");
      return;
    }

    const needsOptions = [2, 3, 4].includes(questionType);
    const options = needsOptions
      ? questionOptions.split("\n").map((option) => option.trim())
      : [];

    if (
      needsOptions &&
      (options.length < 2 || options.some((option) => !option))
    ) {
      setError("Enter at least two non-empty options, one per line.");
      return;
    }

    setSavingQuestion(true);
    setError("");

    try {
      const data = {
        label,
        type: questionType,
        isRequired: questionRequired,
        options,
      };

      if (editingQuestionId) {
        const updatedQuestion = await updateQuestion(
          id,
          editingQuestionId,
          data,
        );

        setQuestions((current) =>
          current.map((question) =>
            question.id === updatedQuestion.id ? updatedQuestion : question,
          ),
        );

        setEditingQuestionId(null);
      } else {
        const createdQuestion = await createQuestion(id, data);
        setQuestions((current) => [...current, createdQuestion]);
      }

      setQuestionLabel("");
      setQuestionType(0);
      setQuestionRequired(false);
      setQuestionOptions("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save question.");
    } finally {
      setSavingQuestion(false);
    }
  }

  async function handleMoveQuestion(
    questionId: string,
    direction: "up" | "down",
  ) {
    const currentIndex = questions.findIndex(
      (question) => question.id === questionId,
    );

    if (currentIndex === -1) return;

    const newIndex = direction === "up" ? currentIndex - 1 : currentIndex + 1;

    if (newIndex < 0 || newIndex >= questions.length) return;

    const reordered = [...questions];
    [reordered[currentIndex], reordered[newIndex]] = [
      reordered[newIndex],
      reordered[currentIndex],
    ];

    const questionIds = reordered.map((question) => question.id);

    setError("");
    setSavingQuestion(true);

    try {
      await reorderQuestions(id!, questionIds);

      setQuestions(
        reordered.map((question, position) => ({
          ...question,
          position,
        })),
      );
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to reorder questions.",
      );
    } finally {
      setSavingQuestion(false);
    }
  }

  async function handleDeleteQuestion(questionId: string) {
    if (!id) {
      setError("Form ID is missing.");
      return;
    }

    const confirmed = window.confirm(
      "Are you sure you want to delete this question?",
    );

    if (!confirmed) {
      return;
    }

    setError("");
    setSavingQuestion(true);

    try {
      await deleteQuestion(id, questionId);

      setQuestions((current) =>
        current.filter((question) => question.id !== questionId),
      );

      if (editingQuestionId === questionId) {
        setEditingQuestionId(null);
        setQuestionLabel("");
        setQuestionType(0);
        setQuestionRequired(false);
        setQuestionOptions("");
      }
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to delete question.",
      );
    } finally {
      setSavingQuestion(false);
    }
  }

  
useEffect(() => {
  if (!id) return;

  const formId: string = id;
  let cancelled = false;

  async function loadEditor() {
    try {
      const [formData, questionData] = await Promise.all([
        getForm(formId),
        getPublishedQuestions(formId),
      ]);

      if (!cancelled) {
        setForm(formData);
        setQuestions(questionData);
        setIsEditing(false);
      }
    } catch (err) {
      if (!cancelled) {
        setError(
          err instanceof Error ? err.message : "Failed to load the form.",
        );
      }
    } finally {
      if (!cancelled) {
        setLoading(false);
      }
    }
  }

  void loadEditor();

  return () => {
    cancelled = true;
  };
}, [id]);


  if (!id) {
    return (
      <main>
        <p role="alert">Form ID is missing.</p>
        <Link to="/">Back to dashboard</Link>
      </main>
    );
  }

  if (loading) {
    return (
      <main>
        <p>Loading form editor...</p>
      </main>
    );
  }

  if (error || !form) {
    return (
      <main>
        <p role="alert">{error || "Form not found."}</p>
        <Link to="/">Back to dashboard</Link>
      </main>
    );
  }

  const questionTypes = [
    "Short text",
    "Long text",
    "Single choice",
    "Multiple choice",
    "Dropdown",
    "Number",
    "Date",
    "Yes / No",
  ];

  return (
    <main>
      <p>
        <Link to="/">← Back to dashboard</Link>
      </p>

      <h1>{form.title}</h1>

      <div>
        <button
          type="button"
          onClick={handlePublish}
          disabled={publishing || savingQuestion || questions.length === 0}
        >
          {publishing ? "Publishing..." : "Publish form"}
        </button>

        <button
          type="button"
          onClick={async () => {
            if (!id) return;

            try {
              setError("");
              await createDraft(id);
              const questionData = await getQuestions(id);
              setQuestions(questionData);
              setIsEditing(true);
            } catch (err) {
              setError(
                err instanceof Error
                  ? err.message
                  : "Failed to enter edit mode.",
              );
            }
          }}
          disabled={isEditing}
        >
          {isEditing ? "Editing form" : "Edit form"}
        </button>

        {publishedVersion !== null && (
          <p role="status">
            Published successfully — version {publishedVersion}.
          </p>
        )}
      </div>

      <h2>Questions</h2>
      {isEditing && (
        <form onSubmit={handleCreateQuestion}>
          <h3>{editingQuestionId ? "Edit question" : "Add a question"}</h3>

          <label htmlFor="questionLabel">Question text</label>
          <input
            id="questionLabel"
            value={questionLabel}
            onChange={(event) => setQuestionLabel(event.target.value)}
            maxLength={1000}
            required
            placeholder="e.g. What is your name?"
          />

          <label htmlFor="questionType">Question type</label>
          <select
            id="questionType"
            value={questionType}
            onChange={(event) => setQuestionType(Number(event.target.value))}
          >
            <option value={0}>Short text</option>
            <option value={1}>Long text</option>
            <option value={2}>Single choice</option>
            <option value={3}>Multiple choice</option>
            <option value={4}>Dropdown</option>
            <option value={5}>Number</option>
            <option value={6}>Date</option>
            <option value={7}>Yes / No</option>
          </select>

          {[2, 3, 4].includes(questionType) && (
            <>
              <label htmlFor="questionOptions">
                Options (one per line; at least two)
              </label>
              <textarea
                id="questionOptions"
                value={questionOptions}
                onChange={(event) => setQuestionOptions(event.target.value)}
                required
                rows={4}
                placeholder={"Option 1\nOption 2"}
              />
            </>
          )}

          <label>
            <input
              type="checkbox"
              checked={questionRequired}
              onChange={(event) => setQuestionRequired(event.target.checked)}
            />
            Required question
          </label>

          <button type="submit" disabled={savingQuestion}>
            {savingQuestion
              ? "Saving..."
              : editingQuestionId
                ? "Save changes"
                : "Add question"}
          </button>

          {editingQuestionId && (
            <button
              type="button"
              onClick={() => {
                setEditingQuestionId(null);
                setQuestionLabel("");
                setQuestionType(0);
                setQuestionRequired(false);
                setQuestionOptions("");
                setError("");
              }}
            >
              Cancel editing
            </button>
          )}
        </form>
      )}
      {error && <p role="alert">{error}</p>}
      {questions.length === 0 ? (
  <p>
    This form hasn't been published yet. Click "Edit form" to add questions
    and publish it.
  </p>
) : (
        <ol>
          {questions.map((question, index) => (
            <li key={question.id}>
              <strong>{question.label}</strong>
              {" — "}
              {questionTypes[question.type] ?? "Unknown type"}

              {question.isRequired && " (Required)"}

              {isEditing && (
                <>
                  <button
                    type="button"
                    onClick={() => {
                      setEditingQuestionId(question.id);
                      setQuestionLabel(question.label);
                      setQuestionType(question.type);
                      setQuestionRequired(question.isRequired);
                      setQuestionOptions(
                        question.options
                          .map((option) => option.label)
                          .join("\n"),
                      );
                      setError("");
                    }}
                  >
                    Edit
                  </button>

                  <button
                    type="button"
                    onClick={() => void handleDeleteQuestion(question.id)}
                    disabled={savingQuestion}
                  >
                    Delete
                  </button>

                  <button
                    type="button"
                    disabled={savingQuestion || index === 0}
                    onClick={() => handleMoveQuestion(question.id, "up")}
                  >
                    Move up
                  </button>

                  <button
                    type="button"
                    disabled={savingQuestion || index === questions.length - 1}
                    onClick={() => handleMoveQuestion(question.id, "down")}
                  >
                    Move down
                  </button>
                </>
              )}

              {question.options.length > 0 && (
                <ul>
                  {question.options.map((option) => (
                    <li key={option.id}>{option.label}</li>
                  ))}
                </ul>
              )}
            </li>
          ))}
        </ol>
      )}
    </main>
  );
}
