export type QcResult = "Passed" | "In progress" | "OOS";

export interface QcTestSummary {
  id: number;
  testObject: string;
  testObjectType: string;
  createdAt: string;
  createdBy: string;
  status: QcResult;
}

export interface QcTestDetail extends QcTestSummary {
  notes?: string;
}

export interface CreateQcTestParameter {
  name: string;
  min: number;
  max: number;
  unit: string;
}

export interface CreateQcTestRequest {
  testObjectType: "Lot" | "Batch";
  testObjectId: number;
  parameters: CreateQcTestParameter[];
  password: string;
}

export interface QcEligibleObject {
  testObjectType: "Lot" | "Batch";
  testObjectId: number;
  label: string;
}

export interface QcEligibleObjectsResponse {
  lots: QcEligibleObject[];
  batches: QcEligibleObject[];
}
