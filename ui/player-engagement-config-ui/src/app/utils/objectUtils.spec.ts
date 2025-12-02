import { ObjectUtils, JsonObject } from './objectUtils';

describe('ObjectUtils', () => {
    describe('stableStringify', () => {
        it('should produce identical strings for objects with different key order', () => {
            const obj1 = { b: 1, a: 2 };
            const obj2 = { a: 2, b: 1 };
            expect(ObjectUtils.stableStringify(obj1)).toBe(ObjectUtils.stableStringify(obj2));
        });

        it('should handle nested objects', () => {
            const obj = { z: { b: 1, a: 2 }, a: 1 };
            expect(ObjectUtils.stableStringify(obj)).toBe('{"a":1,"z":{"a":2,"b":1}}');
        });

        it('should handle null and primitives', () => {
            expect(ObjectUtils.stableStringify(null)).toBe('null');
            expect(ObjectUtils.stableStringify(42)).toBe('42');
            expect(ObjectUtils.stableStringify('hello')).toBe('"hello"');
        });

        it('should preserve empty string', () => {
            expect(ObjectUtils.stableStringify('')).toBe('""');
        });

        it('should handle arrays', () => {
            expect(ObjectUtils.stableStringify([3, 1, 2])).toBe('[3,1,2]');
            expect(ObjectUtils.stableStringify(['c', 'a', 'b'])).toBe('["c","a","b"]');
        });

        it('should handle objects with array values', () => {
            const obj = { b: [1, 2], a: 'test' };
            expect(ObjectUtils.stableStringify(obj)).toBe('{"a":"test","b":[1,2]}');
        });

        it('should handle deeply nested structures', () => {
            const obj1 = { c: { b: { a: 1 } } };
            const obj2 = { c: { b: { a: 1 } } };
            expect(ObjectUtils.stableStringify(obj1)).toBe(ObjectUtils.stableStringify(obj2));
        });

        it('should handle empty objects and arrays', () => {
            expect(ObjectUtils.stableStringify({})).toBe('{}');
            expect(ObjectUtils.stableStringify([])).toBe('[]');
        });

        it('should handle booleans', () => {
            expect(ObjectUtils.stableStringify(true)).toBe('true');
            expect(ObjectUtils.stableStringify(false)).toBe('false');
        });
    });

    describe('deepEqual', () => {
        it('should return true for identical objects', () => {
            const obj1 = { a: 1, b: [1, 2, 3] };
            const obj2 = { a: 1, b: [1, 2, 3] };
            expect(ObjectUtils.deepEqual(obj1, obj2)).toBeTrue();
        });

        it('should return true for arrays with same elements in different order', () => {
            const arr1 = [3, 1, 2];
            const arr2 = [1, 2, 3];
            expect(ObjectUtils.deepEqual(arr1, arr2)).toBeTrue();
        });

        it('should handle numeric arrays correctly (not lexicographic)', () => {
            const arr1 = [1, 10, 2];
            const arr2 = [1, 2, 10];
            expect(ObjectUtils.deepEqual(arr1, arr2)).toBeTrue();
        });

        it('should return false for different values', () => {
            expect(ObjectUtils.deepEqual({ a: 1 }, { a: 2 })).toBeFalse();
        });

        it('should treat empty string and null as equal', () => {
            expect(ObjectUtils.deepEqual('', null)).toBeTrue();
        });

        it('should return true for nested objects with same values, different key order', () => {
            const obj1 = { outer: { b: 2, a: 1 } };
            const obj2 = { outer: { a: 1, b: 2 } };
            expect(ObjectUtils.deepEqual(obj1, obj2)).toBeTrue();
        });

        it('should return true for string arrays with same elements in different order', () => {
            const arr1 = ['c', 'a', 'b'];
            const arr2 = ['a', 'b', 'c'];
            expect(ObjectUtils.deepEqual(arr1, arr2)).toBeTrue();
        });

        it('should handle both inputs as null', () => {
            expect(ObjectUtils.deepEqual(null, null)).toBeTrue();
        });

        it('should handle both inputs as undefined', () => {
            expect(ObjectUtils.deepEqual(undefined, undefined)).toBeTrue();
        });

        it('should return true for empty objects', () => {
            expect(ObjectUtils.deepEqual({}, {})).toBeTrue();
        });

        it('should return true for empty arrays', () => {
            expect(ObjectUtils.deepEqual([], [])).toBeTrue();
        });

        it('should return false for arrays with different lengths', () => {
            expect(ObjectUtils.deepEqual([1, 2], [1, 2, 3])).toBeFalse();
        });

        it('should return false for object vs null', () => {
            expect(ObjectUtils.deepEqual({ a: 1 }, null)).toBeFalse();
        });

        it('should handle arrays of objects (order matters for non-scalar arrays)', () => {
            const arr1 = [{ a: 1 }, { b: 2 }];
            const arr2 = [{ a: 1 }, { b: 2 }];
            expect(ObjectUtils.deepEqual(arr1, arr2)).toBeTrue();
        });

        it('should normalize nested empty strings to null', () => {
            const obj1 = { a: '' };
            const obj2 = { a: null };
            expect(ObjectUtils.deepEqual(obj1, obj2)).toBeTrue();
        });

        it('should handle mixed-type scalar arrays', () => {
            const arr1 = [1, 'a', 2, 'b'];
            const arr2 = ['a', 1, 'b', 2];
            expect(ObjectUtils.deepEqual(arr1, arr2)).toBeTrue();
        });

        it('should normalize arrays within nested objects', () => {
            const obj1 = { nested: { tags: [3, 1, 2] } };
            const obj2 = { nested: { tags: [1, 2, 3] } };
            expect(ObjectUtils.deepEqual(obj1, obj2)).toBeTrue();
        });
    });

    describe('getDiffs', () => {
        it('should return empty objects when items are equal', () => {
            const obj1 = { a: 1, b: 2 } as JsonObject;
            const obj2 = { a: 1, b: 2 } as JsonObject;
            const result = ObjectUtils.getDiffs(obj1, obj2);
            expect(result.left).toEqual({} as JsonObject);
            expect(result.right).toEqual({} as JsonObject);
        });

        it('should return differing fields', () => {
            const obj1 = { a: 1, b: 2 } as JsonObject;
            const obj2 = { a: 1, b: 3 } as JsonObject;
            const result = ObjectUtils.getDiffs(obj1, obj2);
            expect(result.left).toEqual({ b: 2 } as JsonObject);
            expect(result.right).toEqual({ b: 3 } as JsonObject);
        });

        it('should handle missing fields', () => {
            const obj1 = { a: 1 } as JsonObject;
            const obj2 = { a: 1, b: 2 } as JsonObject;
            const result = ObjectUtils.getDiffs(obj1, obj2);
            expect(result.left).toEqual({ b: null } as JsonObject);
            expect(result.right).toEqual({ b: 2 } as JsonObject);
        });

        it('should handle null inputs', () => {
            const result = ObjectUtils.getDiffs(null, { a: 1 } as JsonObject);
            expect(result.left).toEqual({ a: null } as JsonObject);
            expect(result.right).toEqual({ a: 1 } as JsonObject);
        });

        it('should handle both inputs null', () => {
            const result = ObjectUtils.getDiffs(null, null);
            expect(result.left).toEqual({} as JsonObject);
            expect(result.right).toEqual({} as JsonObject);
        });

        it('should handle both inputs undefined', () => {
            const result = ObjectUtils.getDiffs(undefined, undefined);
            expect(result.left).toEqual({} as JsonObject);
            expect(result.right).toEqual({} as JsonObject);
        });

        it('should detect array field differences', () => {
            const obj1 = { tags: [1, 2, 3] } as JsonObject;
            const obj2 = { tags: [1, 2, 4] } as JsonObject;
            const result = ObjectUtils.getDiffs(obj1, obj2);
            expect(result.left).toEqual({ tags: [1, 2, 3] } as JsonObject);
            expect(result.right).toEqual({ tags: [1, 2, 4] } as JsonObject);
        });

        it('should not report difference for arrays with same elements in different order', () => {
            const obj1 = { tags: [3, 1, 2] } as JsonObject;
            const obj2 = { tags: [1, 2, 3] } as JsonObject;
            const result = ObjectUtils.getDiffs(obj1, obj2);
            expect(result.left).toEqual({} as JsonObject);
            expect(result.right).toEqual({} as JsonObject);
        });

        it('should detect multiple differing fields', () => {
            const obj1 = { a: 1, b: 2, c: 3 } as JsonObject;
            const obj2 = { a: 1, b: 5, c: 6 } as JsonObject;
            const result = ObjectUtils.getDiffs(obj1, obj2);
            expect(result.left).toEqual({ b: 2, c: 3 } as JsonObject);
            expect(result.right).toEqual({ b: 5, c: 6 } as JsonObject);
        });

        it('should detect nested object differences', () => {
            const obj1 = { nested: { a: 1 } } as JsonObject;
            const obj2 = { nested: { a: 2 } } as JsonObject;
            const result = ObjectUtils.getDiffs(obj1, obj2);
            expect(result.left).toEqual({ nested: { a: 1 } } as JsonObject);
            expect(result.right).toEqual({ nested: { a: 2 } } as JsonObject);
        });
    });
});