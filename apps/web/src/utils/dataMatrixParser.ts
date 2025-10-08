/**
 * GS1 Data Matrix Parser for Pharmaceutical Products
 * 
 * Parses GS1 Data Matrix barcodes according to Turkish Medicines and Medical Devices Agency standards
 * 
 * Format:
 * - 01<GTIN-14>        → Product code (14 digits)
 * - 21<Serial>         → Serial number (max 20 chars)
 * - 17<YYMMDD>         → Expiry date
 * - 10<Lot>            → Lot/Batch number (max 20 chars)
 * 
 * Example:
 * 01086995460012342115ABC123456171231231012345
 * → GTIN: 08699546001234
 * → Serial: 5ABC123456
 * → Expiry: 2023-12-31
 * → Lot: 12345
 */

export interface DataMatrixResult {
  gtin: string | null;
  serialNumber: string | null;
  expiryDate: string | null; // ISO format: YYYY-MM-DD
  lotNumber: string | null;
  rawData: string;
  isValid: boolean;
  errors: string[];
}

const GS_CHAR = String.fromCharCode(29); // ASCII 29 (Group Separator)

/**
 * Parse GS1 Data Matrix barcode
 */
export function parseDataMatrix(rawData: string): DataMatrixResult {
  const result: DataMatrixResult = {
    gtin: null,
    serialNumber: null,
    expiryDate: null,
    lotNumber: null,
    rawData,
    isValid: false,
    errors: []
  };

  if (!rawData || rawData.trim().length === 0) {
    result.errors.push('Karekod verisi boş');
    return result;
  }

  try {
    let remaining = rawData;

    // 1. GTIN (AI: 01) - Zorunlu
    if (remaining.startsWith('01')) {
      const gtinMatch = remaining.match(/^01(\d{14})/);
      if (gtinMatch) {
        result.gtin = gtinMatch[1];
        remaining = remaining.substring(16); // "01" + 14 digits
      } else {
        result.errors.push('Geçersiz GTIN formatı (14 digit gerekli)');
      }
    } else {
      result.errors.push('GTIN bulunamadı (AI: 01)');
    }

    // Parse remaining fields
    while (remaining.length > 0) {
      // 2. Serial Number (AI: 21) - Değişken uzunluk
      if (remaining.startsWith('21')) {
        remaining = remaining.substring(2); // Remove "21"
        
        // Serial number ends with GS or end of string
        const gsIndex = remaining.indexOf(GS_CHAR);
        const nextAI = remaining.search(/\d{2}/); // Next AI position
        
        if (gsIndex !== -1 && (nextAI === -1 || gsIndex < nextAI)) {
          result.serialNumber = remaining.substring(0, gsIndex);
          remaining = remaining.substring(gsIndex + 1);
        } else if (nextAI !== -1 && nextAI > 0) {
          result.serialNumber = remaining.substring(0, nextAI);
          remaining = remaining.substring(nextAI);
        } else {
          result.serialNumber = remaining;
          remaining = '';
        }
      }
      // 3. Expiry Date (AI: 17) - Sabit 6 digit (YYMMDD)
      else if (remaining.startsWith('17')) {
        const expiryMatch = remaining.match(/^17(\d{6})/);
        if (expiryMatch) {
          const dateStr = expiryMatch[1];
          const yy = parseInt(dateStr.substring(0, 2), 10);
          const mm = parseInt(dateStr.substring(2, 4), 10);
          const dd = parseInt(dateStr.substring(4, 6), 10);
          
          // YY: 00-99 → 2000-2099
          const year = 2000 + yy;
          
          // Validate date
          if (mm >= 1 && mm <= 12 && dd >= 1 && dd <= 31) {
            result.expiryDate = `${year}-${mm.toString().padStart(2, '0')}-${dd.toString().padStart(2, '0')}`;
          } else {
            result.errors.push(`Geçersiz tarih: ${dateStr}`);
          }
          
          remaining = remaining.substring(8); // "17" + 6 digits
        } else {
          result.errors.push('Geçersiz SKT formatı (AI: 17, 6 digit gerekli)');
          remaining = remaining.substring(2); // Skip "17"
        }
      }
      // 4. Lot Number (AI: 10) - Değişken uzunluk
      else if (remaining.startsWith('10')) {
        remaining = remaining.substring(2); // Remove "10"
        
        // Lot number ends with GS or end of string
        const gsIndex = remaining.indexOf(GS_CHAR);
        const nextAI = remaining.search(/\d{2}/); // Next AI position
        
        if (gsIndex !== -1 && (nextAI === -1 || gsIndex < nextAI)) {
          result.lotNumber = remaining.substring(0, gsIndex);
          remaining = remaining.substring(gsIndex + 1);
        } else if (nextAI !== -1 && nextAI > 0) {
          result.lotNumber = remaining.substring(0, nextAI);
          remaining = remaining.substring(nextAI);
        } else {
          result.lotNumber = remaining;
          remaining = '';
        }
      }
      // Unknown AI - Skip
      else {
        const aiMatch = remaining.match(/^(\d{2})/);
        if (aiMatch) {
          result.errors.push(`Bilinmeyen AI: ${aiMatch[1]}`);
          remaining = remaining.substring(2);
        } else {
          // Can't parse, break
          result.errors.push(`Parse edilemeyen veri: ${remaining}`);
          break;
        }
      }
    }

    // Validation
    if (result.gtin) {
      result.isValid = true;
    } else {
      result.errors.push('GTIN zorunludur');
    }

  } catch (error) {
    result.errors.push(`Parse hatası: ${error instanceof Error ? error.message : 'Bilinmeyen hata'}`);
  }

  return result;
}

/**
 * Check if input is likely a Data Matrix barcode
 */
export function isDataMatrix(input: string): boolean {
  if (!input || input.length < 16) return false;
  
  // Check for GS1 Application Identifiers
  const hasAI01 = input.startsWith('01');
  const hasAI21 = input.includes('21');
  const hasAI17 = input.includes('17');
  const hasAI10 = input.includes('10');
  
  // If starts with 01 and has at least one more AI, likely Data Matrix
  return hasAI01 && (hasAI21 || hasAI17 || hasAI10);
}

/**
 * Check if input is a simple GTIN/Barcode (numeric only, 13-14 digits)
 */
export function isSimpleBarcode(input: string): boolean {
  if (!input) return false;
  
  // Remove whitespace
  const cleaned = input.trim();
  
  // Check if numeric and 13-14 digits
  return /^\d{13,14}$/.test(cleaned);
}

/**
 * Extract GTIN from various formats
 */
export function extractGTIN(input: string): string | null {
  if (!input) return null;
  
  // Check if Data Matrix
  if (isDataMatrix(input)) {
    const result = parseDataMatrix(input);
    return result.gtin;
  }
  
  // Check if simple barcode
  if (isSimpleBarcode(input)) {
    const cleaned = input.trim();
    // Pad to 14 digits if needed
    return cleaned.length === 13 ? '0' + cleaned : cleaned;
  }
  
  return null;
}

/**
 * Format Data Matrix result for display
 */
export function formatDataMatrixResult(result: DataMatrixResult): string {
  const parts: string[] = [];
  
  if (result.gtin) parts.push(`GTIN: ${result.gtin}`);
  if (result.serialNumber) parts.push(`Seri: ${result.serialNumber}`);
  if (result.expiryDate) parts.push(`SKT: ${result.expiryDate}`);
  if (result.lotNumber) parts.push(`Lot: ${result.lotNumber}`);
  
  return parts.join(' | ');
}

