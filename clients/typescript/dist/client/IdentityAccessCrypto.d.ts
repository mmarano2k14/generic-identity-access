/** Owns only cryptographic random material and PKCE S256 computation. */
export declare class IdentityAccessCrypto {
    static computeS256Challenge(codeVerifier: string): Promise<string>;
    static randomOpaque(byteLength: number): string;
    private static base64Url;
}
