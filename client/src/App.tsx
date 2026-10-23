import { Navigate, Route, Routes } from "react-router";
import AuthPage from "./features/auth/AuthPage";
import FormEditorPage from "./features/forms/FormEditorPage";

function App() {
  return (
    <Routes>
      <Route path="/" element={<AuthPage />} />
      <Route path="/forms/:id" element={<FormEditorPage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;
