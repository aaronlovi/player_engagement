/**
 * Represents any valid JSON value: primitives, objects, or arrays.
 */
type JsonValue = string | number | boolean | null | JsonObject | JsonValue[];

/**
 * Represents a JSON object with string keys and JSON values.
 */
interface JsonObject {
    [key: string]: JsonValue;
}

/**
 * Result of comparing two JSON objects, containing only the differing fields.
 */
interface DiffResult {
    /** Fields from the left object that differ from the right. */
    left: JsonObject;
    /** Fields from the right object that differ from the left. */
    right: JsonObject;
}

/**
 * Utility class for stable JSON serialization and deep comparison of objects.
 * Provides deterministic stringification regardless of property order.
 */
class ObjectUtils {
    /**
     * Type guard to check if a value is a non-null, non-array object.
     * @param v - The value to check.
     * @returns True if v is a plain object.
     */
    private static isObject(v: unknown): v is JsonObject {
        return !!v && Object.prototype.toString.call(v) === '[object Object]';
    }

    /**
     * Normalizes scalar values by converting empty strings to null.
     * @param v - The value to normalize.
     * @returns The normalized JSON value.
     */
    private static normalizeScalar(v: unknown): JsonValue {
        return v === '' ? null : v as JsonValue;
    }

    /**
     * Normalizes a field value for comparison. Sorts arrays of scalars
     * to ensure order-independent comparison.
     * @param value - The value to normalize.
     * @returns The normalized value.
     */
    private static normalizeField(value: unknown): unknown {
        if (value == null) return value;
        if (Array.isArray(value)) {
            const allScalars = value.every((x) => !this.isObject(x));
            if (allScalars) {
                return [...value].sort((a, b) => {
                    if (typeof a === 'number' && typeof b === 'number')
                        return a - b;
                    return String(a).localeCompare(String(b));
                });
            }
        }
        return value;
    }

    /**
     * Produces a stable JSON string representation of an object.
     * Keys are sorted alphabetically at every level, ensuring identical
     * objects produce identical strings regardless of property insertion order.
     * @param obj - The object to stringify.
     * @returns A deterministic JSON string.
     */
    public static stableStringify(obj: unknown): string {
        if (!this.isObject(obj)) return JSON.stringify(this.normalizeScalar(obj));
        const keys = Object.keys(obj).sort();
        const out: JsonObject = {};
        for (const k of keys) {
            const v = obj[k];
            if (Array.isArray(v)) {
                out[k] = v.map((x) =>
                    this.isObject(x) ? JSON.parse(this.stableStringify(x)) : this.normalizeScalar(x)
                );
            } else if (this.isObject(v)) {
                out[k] = JSON.parse(this.stableStringify(v));
            } else {
                out[k] = this.normalizeScalar(v);
            }
        }
        return JSON.stringify(out);
    }

    /**
     * Performs a deep equality check between two values.
     * Arrays of scalars are compared without regard to order.
     * @param a - First value to compare.
     * @param b - Second value to compare.
     * @returns True if the values are deeply equal after normalization.
     */
    public static deepEqual(a: unknown, b: unknown): boolean {
        const normalizedA = this.normalizeField(a);
        const normalizedB = this.normalizeField(b);
        return this.stableStringify(normalizedA) === this.stableStringify(normalizedB);
    }

    /**
     * Compares two JSON objects and returns only the fields that differ.
     * @param leftItem - The left object to compare (can be null/undefined).
     * @param rightItem - The right object to compare (can be null/undefined).
     * @returns An object containing the differing fields from each side.
     */
    public static getDiffs(
        leftItem: JsonObject | null | undefined,
        rightItem: JsonObject | null | undefined): DiffResult {
        const diffLeft: JsonObject = {};
        const diffRight: JsonObject = {};
        const fields = new Set<string>([
            ...(leftItem ? Object.keys(leftItem) : []),
            ...(rightItem ? Object.keys(rightItem) : []),
        ]);
        for (const f of fields) {
            const lv = leftItem?.[f];
            const rv = rightItem?.[f];
            if (!this.deepEqual(lv, rv)) {
                diffLeft[f] = lv ?? null;
                diffRight[f] = rv ?? null;
            }
        }
        return { left: diffLeft, right: diffRight };
    }
}

export { ObjectUtils };
export type { JsonValue, JsonObject, DiffResult };
