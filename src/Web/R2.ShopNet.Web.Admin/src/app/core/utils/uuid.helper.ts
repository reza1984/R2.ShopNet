/**
 * Utility class for generating and validating UUIDs
 */
export class UuidHelper {

  /**
   * Generates a UUID v7 (timestamp-based, sortable UUID)
   * @returns A UUID v7 string in the format xxxxxxxx-xxxx-7xxx-yxxx-xxxxxxxxxxxx
   */
  static generate(): string {
    return this.v7();
  }

  /**
   * Generates a UUID v7 (timestamp-based, sortable)
   * UUIDv7 features a time-ordered value field derived from the widely implemented Unix Epoch timestamp source
   * @returns A UUID v7 string
   */
  static v7(): string {
    // Get current timestamp in milliseconds
    const timestamp = Date.now();

    // Get random bytes
    const randomBytes = new Uint8Array(10);
    if (typeof crypto !== 'undefined' && crypto.getRandomValues) {
      crypto.getRandomValues(randomBytes);
    } else {
      // Fallback for environments without crypto
      for (let i = 0; i < 10; i++) {
        randomBytes[i] = Math.floor(Math.random() * 256);
      }
    }

    // Convert timestamp to hex (48 bits)
    const timestampHex = timestamp.toString(16).padStart(12, '0');

    // Extract parts of timestamp
    const timeLow = timestampHex.substring(0, 8);
    const timeMid = timestampHex.substring(8, 12);

    // Version (4 bits) = 7, followed by 12 random bits
    const timeHiAndVersion = '7' + Array.from(randomBytes.slice(0, 2))
      .map(b => b.toString(16).padStart(2, '0'))
      .join('')
      .substring(1, 4);

    // Variant (2 bits) = 10, followed by 14 random bits
    const clockSeqAndNode1 = (0x80 | (randomBytes[2] & 0x3f)).toString(16).padStart(2, '0') +
      randomBytes[3].toString(16).padStart(2, '0');

    // 48 random bits for node
    const node = Array.from(randomBytes.slice(4, 10))
      .map(b => b.toString(16).padStart(2, '0'))
      .join('');

    return `${timeLow}-${timeMid}-${timeHiAndVersion}-${clockSeqAndNode1}-${node}`;
  }

  /**
   * Generates a UUID v4 (random UUID)
   * @returns A UUID v4 string in the format xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx
   */
  static v4(): string {
    // Use crypto.randomUUID if available (modern browsers)
    if (typeof crypto !== 'undefined' && crypto.randomUUID) {
      return crypto.randomUUID();
    }

    // Fallback implementation for older environments
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  /**
   * Validates if a string is a valid UUID (v4 or v7)
   * @param uuid The UUID string to validate
   * @returns True if the UUID is valid, false otherwise
   */
  static validate(uuid: string): boolean {
    if (!uuid) return false;

    return this.validateV4(uuid) || this.validateV7(uuid);
  }

  /**
   * Validates if a string is a valid UUID v4
   * @param uuid The UUID string to validate
   * @returns True if the UUID is valid v4, false otherwise
   */
  static validateV4(uuid: string): boolean {
    if (!uuid) return false;

    // UUID v4 pattern: xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx
    // where y is one of [8, 9, a, b]
    const uuidV4Pattern = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
    return uuidV4Pattern.test(uuid);
  }

  /**
   * Validates if a string is a valid UUID v7
   * @param uuid The UUID string to validate
   * @returns True if the UUID is valid v7, false otherwise
   */
  static validateV7(uuid: string): boolean {
    if (!uuid) return false;

    // UUID v7 pattern: xxxxxxxx-xxxx-7xxx-yxxx-xxxxxxxxxxxx
    // where y is one of [8, 9, a, b]
    const uuidV7Pattern = /^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
    return uuidV7Pattern.test(uuid);
  }

  /**
   * Validates if a string is a valid UUID (any version)
   * @param uuid The UUID string to validate
   * @returns True if the UUID is valid, false otherwise
   */
  static validateAny(uuid: string): boolean {
    if (!uuid) return false;

    // General UUID pattern (any version)
    const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    return uuidPattern.test(uuid);
  }

  /**
   * Normalizes a UUID to lowercase format
   * @param uuid The UUID to normalize
   * @returns The normalized UUID string
   */
  static normalize(uuid: string): string {
    if (!this.validateAny(uuid)) {
      throw new Error('Invalid UUID format');
    }
    return uuid.toLowerCase();
  }

  /**
   * Generates a nil UUID (all zeros)
   * @returns A nil UUID string (00000000-0000-0000-0000-000000000000)
   */
  static get empty(): string {
    return '00000000-0000-0000-0000-000000000000';
  }

  /**
   * Checks if a UUID is nil (all zeros)
   * @param uuid The UUID to check
   * @returns True if the UUID is nil, false otherwise
   */
  static isNil(uuid: string): boolean {
    return uuid === this.empty;
  }
}
