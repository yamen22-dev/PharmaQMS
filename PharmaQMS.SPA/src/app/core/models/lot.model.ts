export type LotStatus = "Quarantine" | "Released" | "Rejected";

export interface LotSummary {
  id: number;
  lotNumber: string;
  rawMaterialId: number;
  rawMaterialName: string;
  supplier: string;
  quantity: number;
  unit: string;
  receivedDateUtc: string;
  expiryDateUtc: string;
  status: LotStatus;
}

export interface LotDetail {
  id: number;
  lotNumber: string;
  rawMaterialId: number;
  rawMaterialName: string;
  supplier: string;
  pharmaceuticalApi: string;
  quantity: number;
  unit: string;
  receivedDateUtc: string;
  minSpecificationLimit: number;
  maxSpecificationLimit: number;
  expiryDateUtc: string;
  status: LotStatus;
}

export interface CreateLotRequest {
  rawMaterialId: number;
  lotNumber: string;
  supplier: string;
  quantity: number;
  receivedDateUtc: string;
  expiryDateUtc: string;
  purchaseOrderNumber: string;
  analysisCertificate: string;
}

export interface ChangeLotStatusRequest {
  newStatus: LotStatus;
  reason: string;
  password: string;
}

export interface LotStatusChangedResponse {
  id: number;
  lotNumber: string;
  oldStatus: LotStatus;
  newStatus: LotStatus;
}
