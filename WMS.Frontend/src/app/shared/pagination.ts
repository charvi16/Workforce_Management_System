export interface NormalizedPage<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export function normalizePagedResponse<T>(
  response: unknown,
  fallbackPageNumber: number,
  fallbackPageSize: number
): NormalizedPage<T> {
  const normalized = toCamelCaseObject(response) as Record<string, unknown> | unknown[];
  const payload = unwrapPayload(normalized);
  const items = readItems<T>(payload);
  const page = isRecord(payload) ? payload : {};
  const pageSize = readNumber(page, 'pageSize', fallbackPageSize);
  const totalCount = readNumber(page, 'totalCount', items.length);
  const totalPages = readNumber(page, 'totalPages', pageSize > 0 ? Math.ceil(totalCount / pageSize) : 0);

  return {
    items,
    totalCount,
    pageNumber: readNumber(page, 'pageNumber', fallbackPageNumber),
    pageSize,
    totalPages
  };
}

export function toCamelCaseObject(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map((item) => toCamelCaseObject(item));
  }

  if (!isRecord(value)) {
    return value;
  }

  return Object.entries(value).reduce<Record<string, unknown>>((result, [key, item]) => {
    const normalizedKey = key.length ? `${key[0].toLowerCase()}${key.slice(1)}` : key;
    result[normalizedKey] = toCamelCaseObject(item);
    return result;
  }, {});
}

function unwrapPayload(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value;
  }

  if (!isRecord(value)) {
    return {};
  }

  return value['data'] ?? value;
}

function readItems<T>(payload: unknown): T[] {
  if (Array.isArray(payload)) {
    return payload as T[];
  }

  if (!isRecord(payload)) {
    return [];
  }

  const candidates = [
    payload['items'],
    payload['$values'],
    payload['records'],
    payload['results'],
    payload['values']
  ];

  for (const candidate of candidates) {
    if (Array.isArray(candidate)) {
      return candidate as T[];
    }

    if (isRecord(candidate) && Array.isArray(candidate['$values'])) {
      return candidate['$values'] as T[];
    }
  }

  return [];
}

function readNumber(source: Record<string, unknown>, key: string, fallback: number): number {
  const value = Number(source[key] ?? fallback);
  return Number.isFinite(value) ? value : fallback;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return !!value && typeof value === 'object' && !Array.isArray(value);
}
