import { describe, it, expect } from '@jest/globals';
import {
  parseDataMatrix,
  isDataMatrix,
  isSimpleBarcode,
  extractGTIN,
  formatDataMatrixResult
} from '../dataMatrixParser';

describe('Data Matrix Parser', () => {
  describe('parseDataMatrix', () => {
    it('should parse full Data Matrix with all fields', () => {
      const input = '01086995460012342115ABC123456171231231012345';
      const result = parseDataMatrix(input);

      expect(result.isValid).toBe(true);
      expect(result.gtin).toBe('08699546001234');
      expect(result.serialNumber).toBe('5ABC123456');
      expect(result.expiryDate).toBe('2023-12-31');
      expect(result.lotNumber).toBe('12345');
      expect(result.errors).toHaveLength(0);
    });

    it('should parse Data Matrix with only GTIN and Serial', () => {
      const input = '01086995460012342115ABC123456';
      const result = parseDataMatrix(input);

      expect(result.isValid).toBe(true);
      expect(result.gtin).toBe('08699546001234');
      expect(result.serialNumber).toBe('15ABC123456');
      expect(result.expiryDate).toBeNull();
      expect(result.lotNumber).toBeNull();
    });

    it('should parse Data Matrix with only GTIN and Expiry', () => {
      const input = '0108699546001234172312311012345';
      const result = parseDataMatrix(input);

      expect(result.isValid).toBe(true);
      expect(result.gtin).toBe('08699546001234');
      expect(result.serialNumber).toBeNull();
      expect(result.expiryDate).toBe('2023-12-31');
      expect(result.lotNumber).toBe('12345');
    });

    it('should handle empty input', () => {
      const result = parseDataMatrix('');

      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('Karekod verisi boş');
    });

    it('should handle invalid GTIN format', () => {
      const result = parseDataMatrix('01123'); // Too short

      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('Geçersiz GTIN formatı (14 digit gerekli)');
    });

    it('should handle missing GTIN', () => {
      const result = parseDataMatrix('2115ABC123456');

      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('GTIN bulunamadı (AI: 01)');
    });

    it('should handle invalid expiry date', () => {
      const result = parseDataMatrix('0108699546001234171399'); // 13th month

      expect(result.isValid).toBe(true); // Still valid because GTIN exists
      expect(result.errors.length).toBeGreaterThan(0);
    });
  });

  describe('isDataMatrix', () => {
    it('should detect valid Data Matrix', () => {
      expect(isDataMatrix('01086995460012342115ABC123456')).toBe(true);
      expect(isDataMatrix('010869954600123417231231')).toBe(true);
      expect(isDataMatrix('01086995460012341012345')).toBe(true);
    });

    it('should reject invalid Data Matrix', () => {
      expect(isDataMatrix('')).toBe(false);
      expect(isDataMatrix('123')).toBe(false);
      expect(isDataMatrix('8699546001234')).toBe(false); // Simple barcode
      expect(isDataMatrix('ABC123')).toBe(false);
    });
  });

  describe('isSimpleBarcode', () => {
    it('should detect valid EAN-13', () => {
      expect(isSimpleBarcode('8699546001234')).toBe(true);
    });

    it('should detect valid GTIN-14', () => {
      expect(isSimpleBarcode('08699546001234')).toBe(true);
    });

    it('should reject invalid barcodes', () => {
      expect(isSimpleBarcode('')).toBe(false);
      expect(isSimpleBarcode('123')).toBe(false);
      expect(isSimpleBarcode('ABC123')).toBe(false);
      expect(isSimpleBarcode('123456789012345')).toBe(false); // 15 digits
    });
  });

  describe('extractGTIN', () => {
    it('should extract GTIN from Data Matrix', () => {
      expect(extractGTIN('01086995460012342115ABC123456')).toBe('08699546001234');
    });

    it('should extract GTIN from simple barcode', () => {
      expect(extractGTIN('8699546001234')).toBe('08699546001234'); // Padded to 14
      expect(extractGTIN('08699546001234')).toBe('08699546001234');
    });

    it('should return null for invalid input', () => {
      expect(extractGTIN('')).toBeNull();
      expect(extractGTIN('ABC123')).toBeNull();
    });
  });

  describe('formatDataMatrixResult', () => {
    it('should format complete result', () => {
      const result = parseDataMatrix('01086995460012342115ABC123456171231231012345');
      const formatted = formatDataMatrixResult(result);

      expect(formatted).toContain('GTIN: 08699546001234');
      expect(formatted).toContain('Seri: 5ABC123456');
      expect(formatted).toContain('SKT: 2023-12-31');
      expect(formatted).toContain('Lot: 12345');
    });

    it('should format partial result', () => {
      const result = parseDataMatrix('01086995460012342115ABC123456');
      const formatted = formatDataMatrixResult(result);

      expect(formatted).toContain('GTIN: 08699546001234');
      expect(formatted).toContain('Seri: 15ABC123456');
      expect(formatted).not.toContain('SKT:');
      expect(formatted).not.toContain('Lot:');
    });
  });
});

