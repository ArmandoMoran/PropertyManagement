export interface Property {
  propertyId: number;
  fullAddress: string;
  street: string;
  city: string;
  state: string;
  zipCode: string;
  owner?: string;
  propertyType?: string;
  units?: number;
  sqFt?: number;
  zestimate?: number;
}

export interface PropertyListItem {
  propertyId: number;
  fullAddress: string;
  shortName: string;
}

export interface Lender {
  lenderId: number;
  propertyId: number;
  lenderName: string;
  lenderUrl?: string;
  userId?: string;
  mortgageNumber?: string;
  monthlyPayment: number;
  effectiveDate?: string;
}

export interface HoaInfo {
  hoaId: number;
  propertyId: number;
  hoaName: string;
  accountNumber?: string;
  managementCompany?: string;
  paymentFrequency?: string;
  paymentAmount: number;
  effectiveYear?: number;
}

export interface Insurance {
  insuranceId: number;
  propertyId: number;
  carrier: string;
  policyNumber?: string;
  renewalDate?: string;
  whoPays?: string;
}

export interface InsurancePremium {
  premiumId: number;
  insuranceId: number;
  policyYear: number;
  annualPremium: number;
  yoyPercentChange?: number;
}

export interface PrincipalBalance {
  balanceId: number;
  propertyId: number;
  snapshotDate: string;
  principalBalance: number;
}

export interface PropertyHistory {
  historyId: number;
  propertyId: number;
  eventDate: string;
  propertyName?: string;
  description?: string;
  notes?: string;
  createdDate?: string;
}

export interface PropertyDetail extends Property {
  lender?: Lender;
  hoa?: HoaInfo;
  insurance?: Insurance;
  insurancePremiums: InsurancePremium[];
  allLenders: Lender[];
  principalBalanceHistory: PrincipalBalance[];
  propertyHistory: PropertyHistory[];
}

export interface Transaction {
  transactionId: number;
  transactionDate: string;
  name?: string;
  notes?: string;
  details?: string;
  category?: string;
  subCategory?: string;
  amount: number;
  portfolio?: string;
  propertyId?: number;
  propertyRaw?: string;
  unit?: string;
  dataSource?: string;
  account?: string;
  owner?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  expiration: string;
}
