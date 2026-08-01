import { tokenStorage } from "@/lib/auth/storage";
import type { ProblemDetails } from "@/lib/api/types";

const API_URL =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/$/, "") ||
  "http://localhost:5240/api";

export class ApiError extends Error {
  status: number;
  problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(formatProblemMessage(problem, status));
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  get code() {
    return this.problem.errorCode || this.problem.type?.split("/").pop();
  }
}

/** Prefer field-level validation messages over generic "One or more validation errors". */
function formatProblemMessage(problem: ProblemDetails, status: number): string {
  const fieldMessages = flattenValidationErrors(problem.errors);
  if (fieldMessages.length > 0) {
    return fieldMessages.join(" · ");
  }

  const detail = problem.detail?.trim();
  if (
    detail &&
    !/^one or more validation errors/i.test(detail)
  ) {
    return detail;
  }

  return problem.title || detail || `HTTP ${status}`;
}

function flattenValidationErrors(
  errors?: Record<string, string[]>,
): string[] {
  if (!errors || typeof errors !== "object") return [];
  const out: string[] = [];
  for (const msgs of Object.values(errors)) {
    if (Array.isArray(msgs)) {
      for (const m of msgs) {
        if (typeof m === "string" && m.trim()) out.push(m.trim());
      }
    }
  }
  return out;
}

type RequestOptions = {
  method?: string;
  body?: unknown;
  auth?: boolean;
  headers?: Record<string, string>;
  /** Skip refresh retry once */
  _retried?: boolean;
};

async function parseProblem(res: Response): Promise<ProblemDetails> {
  try {
    const data = (await res.json()) as ProblemDetails;
    return data;
  } catch {
    return {
      status: res.status,
      title: res.statusText,
      detail: `Request failed with status ${res.status}`,
    };
  }
}

let refreshPromise: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  const refresh = tokenStorage.getRefresh();
  if (!refresh) return false;

  if (!refreshPromise) {
    refreshPromise = (async () => {
      try {
        const res = await fetch(`${API_URL}/auth/refresh-token`, {
          method: "POST",
          headers: { "Content-Type": "application/json", Accept: "application/json" },
          body: JSON.stringify({ refreshToken: refresh }),
        });
        if (!res.ok) {
          tokenStorage.clear();
          return false;
        }
        const data = (await res.json()) as {
          accessToken: string;
          refreshToken: string;
        };
        const user = tokenStorage.getUser();
        if (user) {
          tokenStorage.setSession(data.accessToken, data.refreshToken, user);
        } else {
          tokenStorage.setAccess(data.accessToken);
        }
        return true;
      } catch {
        tokenStorage.clear();
        return false;
      } finally {
        refreshPromise = null;
      }
    })();
  }
  return refreshPromise;
}

export async function apiRequest<T>(
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const { method = "GET", body, auth = true, headers = {}, _retried } = options;
  const url = path.startsWith("http")
    ? path
    : `${API_URL}${path.startsWith("/") ? path : `/${path}`}`;

  const reqHeaders: Record<string, string> = {
    Accept: "application/json",
    ...headers,
  };

  if (body !== undefined) {
    reqHeaders["Content-Type"] = "application/json";
  }

  if (auth) {
    const token = tokenStorage.getAccess();
    if (token) reqHeaders.Authorization = `Bearer ${token}`;
  }

  const res = await fetch(url, {
    method,
    headers: reqHeaders,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (res.status === 401 && auth && !_retried) {
    const ok = await tryRefresh();
    if (ok) {
      return apiRequest<T>(path, { ...options, _retried: true });
    }
  }

  if (res.status === 204) {
    return undefined as T;
  }

  if (!res.ok) {
    throw new ApiError(res.status, await parseProblem(res));
  }

  if (res.status === 304) {
    return undefined as T;
  }

  const text = await res.text();
  if (!text) return undefined as T;
  return JSON.parse(text) as T;
}

export const api = {
  get: <T>(path: string, auth = true) =>
    apiRequest<T>(path, { method: "GET", auth }),
  post: <T>(path: string, body?: unknown, auth = true) =>
    apiRequest<T>(path, { method: "POST", body, auth }),
  put: <T>(path: string, body?: unknown, auth = true) =>
    apiRequest<T>(path, { method: "PUT", body, auth }),
  patch: <T>(path: string, body?: unknown, auth = true) =>
    apiRequest<T>(path, { method: "PATCH", body, auth }),
  delete: <T>(path: string, auth = true) =>
    apiRequest<T>(path, { method: "DELETE", auth }),
};

export { API_URL };
