export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errorCode?: string;
  errors?: Record<string, string[]>;
};

export type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  expiration: string;
  user: {
    id: string;
    email: string;
    fullName: string;
    locale: string;
    roles: string[];
    isSetupComplete?: boolean | null;
  };
};

export type PaginationInfo = {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type SubscriptionSummary = {
  monthlyTotal: number;
  yearlyTotal: number;
  currency: string;
  monthlyBudget?: number | null;
  isBudgetExceeded: boolean;
  warnings?: string[];
  hasUnconvertedAmounts?: boolean;
};

export type SubscriptionItem = {
  id: string;
  name: string;
  price: number;
  currency: string;
  /** "monthly" | "yearly" (or legacy enum number 1/2 from older API builds) */
  billingCycle: string | number;
  sharedWithCount: number;
  userShare: number;
  monthlyEquivalentShare: number;
  nextRenewalDate: string;
  lastUsedAt?: string | null;
  notes?: string | null;
  archived: boolean;
  categoryId?: string | null;
  userCategoryId?: string | null;
  providerId?: string | null;
  category?: { id: string; slug: string; name: string; color?: string | null } | null;
  provider?: { id: string; name: string; slug: string } | null;
};

export type ListSubscriptionsResponse = {
  data: SubscriptionItem[];
  pagination: PaginationInfo;
  summary: SubscriptionSummary;
};

export type UpcomingItem = {
  id: string;
  name: string;
  price: number;
  currency: string;
  nextRenewalDate: string;
  daysUntilRenewal: number;
  isOverdue: boolean;
  isUpcoming: boolean;
  monthlyEquivalentShare?: number;
};

export type ActivityItem = {
  id: string;
  entityType: string;
  entityId?: string | null;
  action: string;
  description: string;
  createdAt: string;
};

export type ProfileResponse = {
  id: string;
  email: string;
  fullName: string;
  locale: string;
  mainCurrency: string;
  monthlyBudget?: number | null;
  applicationThemeColor: string;
  darkTheme: boolean;
};

/** GET/PUT /api/profile/notifications — email send is Faz 15 (always false). */
export type NotificationSettingsResponse = {
  emailEnabled: boolean;
  pushEnabled: boolean;
  daysBeforeRenewal: number;
};

export type CategoryItem = {
  id: string;
  slug: string;
  name: string;
  icon?: string | null;
  color?: string | null;
  sortOrder: number;
};

export type ProviderItem = {
  id: string;
  name: string;
  slug: string;
  currency?: string;
  price?: number | null;
};

export type SetupStatus = {
  isSetupComplete: boolean;
  hasSuperAdmin?: boolean;
  canCreateAdmin?: boolean;
  allowPublicRegistration?: boolean;
  suggestedNextStep?: string | null;
  instanceName?: string | null;
  defaultLocale?: string | null;
  defaultCurrency?: string | null;
  hasSmtpConfigured?: boolean;
  hasAiConfigured?: boolean;
  version?: string;
  defaultApplicationThemeColor?: string | null;
  defaultDarkTheme?: boolean;
};

export type CreateSetupAdminResponse = {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  accessToken?: string | null;
  refreshToken?: string | null;
  expiration?: string | null;
};

export type AdminUser = {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
  isLockedOut: boolean;
  isDisabled: boolean;
  createdAt: string;
  activeSubscriptionCount: number;
};

export type SystemSettingsResponse = {
  instance: {
    instanceName?: string | null;
    defaultLocale: string;
    defaultCurrency: string;
    timeZoneId?: string | null;
    allowPublicRegistration: boolean;
    isSetupComplete: boolean;
    defaultApplicationThemeColor?: string;
    defaultDarkTheme?: boolean;
  };
  ai: {
    provider?: string | null;
    model?: string | null;
    baseUrl?: string | null;
    hasApiKey: boolean;
    apiKeyMasked?: string | null;
  };
  smtp: {
    enabled: boolean;
    host?: string | null;
    port?: number | null;
    user?: string | null;
    hasPassword: boolean;
    fromName?: string | null;
    fromEmail?: string | null;
  };
};

export type MonthlySpendResponse = {
  data: { month: string; total: number }[];
  currency: string;
  average: number;
  message?: string | null;
};

export type CategoryBreakdownResponse = {
  data: {
    category: string;
    name: string;
    color?: string | null;
    total: number;
    percentage: number;
    count: number;
  }[];
  grandTotal: number;
  currency: string;
  message?: string | null;
};

export type AiAnalyzeResponse = {
  summary: string;
  tips: {
    type: string;
    message: string;
    potentialSaving?: number | null;
    subscriptionId?: string | null;
    subscriptionName?: string | null;
  }[];
  estimatedMonthlySaving: number;
  estimatedYearlySaving: number;
  analyzedAt: string;
};

export type AiHistoryResponse = {
  data: {
    id: string;
    summary: string;
    estimatedMonthlySaving: number;
    estimatedYearlySaving: number;
    createdAt: string;
  }[];
  pagination: PaginationInfo;
};

/** GET /api/ai/history/{id} — full stored analysis */
export type AiHistoryDetailResponse = AiAnalyzeResponse & {
  id: string;
  createdAt: string;
};
