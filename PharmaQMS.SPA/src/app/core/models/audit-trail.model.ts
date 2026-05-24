export interface AuditTrailEntry {
  id: number;
  tijdstip: string;
  gebruikerId: string;
  actie: string;
  entiteitType: string;
  entiteitId: string;
  oudWaarde: string | null;
  nieuweWaarde: string | null;
  ipAdres: string;
}