export interface UserSettings {
  id: number;
  userId: string;
  openAiApiKey?: string;
  openAiModel: string;
  devOpsPersonalAccessToken?: string;
  createdAt: Date;
  updatedAt: Date;
}
