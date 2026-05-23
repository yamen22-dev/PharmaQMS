export interface BmrDetailResponse {
  Id: string;
  BatchNumber: string;
  RecipeName: string;
  RecipeVersion: string;
  ProductionLineName: string;
  BatchSize: number;
  Status: BmrStatus;
  CreatedAt: Date;
  Lots: ReadonlySet<BmrLotLinkResponse>;
  Steps: ReadonlySet<BmrStepResponse>;
}
export interface BmrListQuery {
  Status?: BmrStatus;
  Page: number;
  PageSize: number;
}
export interface BmrLotLinkResponse {
  LotId: number;
  LotNumber: string;
  RawMaterialName: string;
}
export interface BmrStepResponse {
  Id: string;
  StepNumber: number;
  StepName: string;
  IsCritical: boolean;
  Status: BmrStepStatus;
  EnteredData?: string;
  EnteredById?: string;
  EnteredAt?: Date;
  VerifiedById?: string;
  VerifiedAt?: Date;
  DeviationNote?: string;
}

export interface BmrSummaryResponse {
  productName: any;
  Id: string;
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
