export enum RawMaterialCategory {
  ActivePharmaceuticalIngredient = 'ActivePharmaceuticalIngredient',
  Excipient = 'Excipient',
  Packaging = 'Packaging',
  Solvent = 'Solvent',
  Other = 'Other'
}

export type LotStatus = "Quarantine" | "Released" | "Rejected";

export interface RawMaterialOverview {
  id: number;
  name: string;
  pharmaceuticalApi: string;
  category: string;
  unit: string;
  activeLots: number;
  quarantineLots: number;
}

export interface RawMaterialLotSummary {
  id: number;
  lotNumber: string;
  status: LotStatus;
}

export interface RawMaterialDetail {
  id: number;
  name: string;
  pharmaceuticalApi: string;
  category: string;
  unit: string;
  minSpecificationLimit: number;
  maxSpecificationLimit: number;
  notes: string;
  lots: RawMaterialLotSummary[];
}

export interface CreateRawMaterialRequest {
  name: string;
  pharmaceuticalApi: string;
  category: RawMaterialCategory;
  unit: string;
  minSpecificationLimit: number;
  maxSpecificationLimit: number;
  supplier: string;
  cepNumber?: string;
  notes?: string;
}

export interface RawMaterialResponse {
  id: number;
  name: string;
  pharmaceuticalApi: string;
  category: string;
  unit: string;
  minSpecificationLimit: number;
  maxSpecificationLimit: number;
  supplier: string;
  cepNumber?: string;
  notes?: string;
  createdUtc: string;
}
