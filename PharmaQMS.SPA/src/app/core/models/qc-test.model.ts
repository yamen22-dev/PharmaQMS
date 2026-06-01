export type QcResult = "Passed" | "In progress" | "OOS";

export interface QcTestSummary {
  id: number;
  testObject: string;
  testObjectType: string;
  testObjectId: number;
  createdAt: string;
  createdBy: string;
  status: QcResult;
}

export interface QcTestParameter {
  id: number;
  name: string;
  unit: string;
  min: number;
  max: number;
  measuredValue: number | null;
  isWithinSpecification: boolean;
}

export interface QcTestDetail extends QcTestSummary {
  testObjectLabel: string;
  parameters: QcTestParameter[];
}

export interface SubmitQcTestResultParameter {
  parameterId: number;
  measuredValue: number | null;
}

export interface SubmitQcTestResultsRequest {
  password: string;
  parameters: SubmitQcTestResultParameter[];
}

export interface SubmitQcTestResultsResponse {
  id: number;
  testObjectType: string;
  testObjectId: number;
  testObjectLabel: string;
  status: QcResult;
  message: string;
  coaStarted: boolean;
  notificationQueued: boolean;
  parameters: QcTestParameter[];
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
