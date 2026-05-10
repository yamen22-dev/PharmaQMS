export enum RawMaterialCategory {
  ActivePharmaceuticalIngredient = 'ActivePharmaceuticalIngredient',
  Excipient = 'Excipient',
  Packaging = 'Packaging',
  Solvent = 'Solvent',
  Other = 'Other'
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
