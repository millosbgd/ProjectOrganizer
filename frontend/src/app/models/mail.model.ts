export interface Mail {
  id: number;
  userId: number;
  messageId: string;
  from: string;
  cc?: string;
  subject: string;
  bodyText?: string;
  bodyHtml?: string;
  receivedDateTime: string;
  datumUcitavanja: string;
}

export interface FetchMailsRequest {
  accessToken: string;
  days: number;
}

export interface FetchMailsResult {
  newCount: number;
  totalFetched: number;
  message: string;
}
