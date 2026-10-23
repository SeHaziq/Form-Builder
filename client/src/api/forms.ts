
import { apiRequest } from "./api";

export interface FormSummary {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
}

export interface CreateFormRequest {
    title: string;
}

export function getForms(): Promise<FormSummary[]> {
    return apiRequest<FormSummary[]>("/forms");
}

export function createForm(
    data: CreateFormRequest
): Promise<FormSummary> {
    return apiRequest<FormSummary>("/forms", {
        method: "POST",
        body: JSON.stringify(data),
    });
}

export function getForm(id: string): Promise<FormSummary> {
    return apiRequest<FormSummary>(`/forms/${id}`);
}


export interface FormQuestion {
    id: string;
    formVersionId: string;
    label: string;
    type: number;
    isRequired: boolean;
    position: number;
    options: {
        id: string;
        label: string;
        position: number;
    }[];
}

export interface CreateQuestionRequest {
    label: string;
    type: number;
    isRequired: boolean;
    options: string[];
}

export function getQuestions(formId: string): Promise<FormQuestion[]> {
    return apiRequest<FormQuestion[]>(`/forms/${formId}/questions`);
}


export function getPublishedQuestions(
  formId: string,
): Promise<FormQuestion[]> {
  return apiRequest<FormQuestion[]>(
    `/forms/${formId}/published-questions`,
  );
}


export function createQuestion(
    formId: string,
    data: CreateQuestionRequest,
): Promise<FormQuestion> {
    return apiRequest<FormQuestion>(`/forms/${formId}/questions`, {
        method: "POST",
        body: JSON.stringify(data),
    });
}


export interface FormQuestion {
    id: string;
    formVersionId: string;
    label: string;
    type: number;
    isRequired: boolean;
    position: number;
    options: {
        id: string;
        label: string;
        position: number;
    }[];
}

export interface CreateQuestionRequest {
    label: string;
    type: number;
    isRequired: boolean;
    options: string[];
}


export function updateQuestion(
    formId: string,
    questionId: string,
    data: CreateQuestionRequest,
): Promise<FormQuestion> {
    return apiRequest<FormQuestion>(
        `/forms/${formId}/questions/${questionId}`,
        {
            method: "PUT",
            body: JSON.stringify(data),
        },
    );
}


export function deleteQuestion(
    formId: string,
    questionId: string,
): Promise<void> {
    return apiRequest<void>(
        `/forms/${formId}/questions/${questionId}`,
        {
            method: "DELETE",
        },
    );
}

export function reorderQuestions(
  formId: string,
  questionIds: string[],
): Promise<void> {
  return apiRequest<void>(`/forms/${formId}/questions/reorder`, {
    method: "PUT",
    body: JSON.stringify({ questionIds }),
  });
}


export interface PublishFormResult {
  formId: string;
  versionId: string;
  versionNumber: number;
  status: string;
  publishedAt: string;
}

export function publishForm(
  formId: string,
): Promise<PublishFormResult> {
  return apiRequest<PublishFormResult>(
    `/forms/${formId}/publish`,
    {
      method: "POST",
    },
  );
}


export interface DraftFormResult {
  versionId: string;
  versionNumber: number;
  status: string;
}

export function createDraft(
  formId: string,
): Promise<DraftFormResult> {
  return apiRequest<DraftFormResult>(
    `/forms/${formId}/draft`,
    {
      method: "POST",
    },
  );
}
