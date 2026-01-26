export interface UserSettings {
  id: number;
  userId: string;
  openAiApiKey?: string;
  openAiModel: string;
  createdAt: Date;
  updatedAt: Date;
}
