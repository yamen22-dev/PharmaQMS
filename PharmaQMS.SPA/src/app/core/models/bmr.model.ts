export interface BmrDetailResponse {
  id: string;
  batchNumber: string;
  recipeName: string;
  recipeVersion: string;
  productionLineName: string;
  batchSize: number;
  status: BmrStatus;
  createdAt: string;
  lots: BmrLotLinkResponse[];
  steps: BmrStepResponse[];
}
export interface BmrListQuery {
  Status?: BmrStatus;
  Page: number;
  PageSize: number;
}
export interface BmrLotLinkResponse {
  lotId: number;
  lotNumber: string;
  rawMaterialName: string;
}
export interface BmrStepResponse {
  id: string;
  stepNumber: number;
  stepName: string;
  isCritical: boolean;
  status: BmrStepStatus;
  enteredData?: string | null;
  enteredById?: string | null;
  enteredAt?: string | null;
  verifiedById?: string | null;
  verifiedAt?: string | null;
  deviationNote?: string | null;
}

export interface BmrSummaryResponse {
  productName: any;
  id: string;
  batchNumber: string;
  recipeName: string;
  productionLineName: string;
  createdAt: Date;
  status: BmrStatus;
  totalSteps: number;
  completedSteps: number;
}

export interface ConfirmStepRequest {
  EnteredData: string;
  DeviationNote?: string;
}

export interface CreateBmrRequest {
  MasterRecipeId: string;
  BatchSize: number;
  ProductionLineId: string;
  LotIds: Array<number>;
}

export interface PageResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
export interface VerifyStepRequest {
    Password: string;
}

export enum BmrStatus {
  InProgress = 0,
  Completed = 1,
  Rejected = 2,
  InQc = 3,
}

export enum BmrStepStatus {
  Open = 0,
  AwaitingVerification = 1,
  Verified = 2,
}
