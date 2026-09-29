export type IdentityJsonObject = Record<string, unknown>;
/** Central scalar validation/normalization used by specialized client classes. */
export declare class IdentityAccessValueCodec {
    static object(value: unknown): IdentityJsonObject;
    static text(value: unknown): string;
    static flag(value: unknown): boolean;
    static positiveInteger(value: unknown): number;
    static timestamp(value: unknown): string;
    static nonEmpty(value: string): string;
    static nonEmptySecret(value: string): string;
    static token(value: string): string;
    static opaqueToken43(value: string): string;
    static uuid(value: string): string;
    static clientId(value: string): string;
    static capabilityPatternSegment(value: string): string;
    static managedPolicyKey(value: string): string;
    static slug(value: string): string;
    static rbacContextSegment(value: string): string;
    static sha256(value: unknown): string;
    static redirectUri(value: string): string;
    static opaqueState(value: string): string;
    static nonce(value: string): string;
    static pkceVerifier(value: string): string;
    static optionalUuidOrEmpty(value: string | undefined): string;
    static lifecycleStatus(value: unknown): 1 | 2;
    static version(value: unknown): number;
    static boolean(value: unknown): boolean;
    static nullableUuid(value: unknown): string | undefined;
    static array<T>(value: unknown, decode: (entry: unknown) => T): readonly T[];
}
