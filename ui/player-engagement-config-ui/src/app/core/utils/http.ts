// ApiResult captures the raw outcome of an HTTP call (success/failure plus payload/error).
export interface ApiResult<T> {
  ok: boolean;
  status: number | null;
  body: T | null;
  error?: string | null;
}

// ApiState captures UI state for a given request as it moves through loading/success/error.
export interface ApiState<T> {
  loading: boolean;
  status: number | null;
  success: boolean | null;
  body: T | null;
  error: string | null;
}

// Creates a clean initial state for a given request.
export function createInitialState<T>(): ApiState<T> {
  return {
    loading: false,
    status: null,
    success: null,
    body: null,
    error: null
  };
}

// Maps an ApiResult into an ApiState shape. Useful for reducing boilerplate in components.
export function toApiState<T>(result: ApiResult<T>, loading = false): ApiState<T> {
  return {
    loading,
    status: result.status,
    success: result.ok,
    body: result.body,
    error: result.ok ? null : result.error ?? null
  };
}
