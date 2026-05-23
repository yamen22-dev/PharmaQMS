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
