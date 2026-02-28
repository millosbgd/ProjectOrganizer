// Response DTO from backend - contains masked keys
export interface UserSettings {
  id: number;
  userId: string;
  openAiApiKeyMasked?: string | null;
  hasOpenAiApiKey: boolean;
  openAiModel: string;
  devOpsPatMasked?: string | null;
  hasDevOpsPat: boolean;
  createdAt: Date;
  updatedAt: Date;
}

// DTO for updating settings - only send keys when user wants to change them
export interface UpdateUserSettings {
  openAiApiKey?: string | null;  // Only send when updating, use "REMOVE" to delete
  openAiModel: string;
  devOpsPersonalAccessToken?: string | null;  // Only send when updating, use "REMOVE" to delete
}

