import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-fetch-data',
  templateUrl: './fetch-data.component.html'
})
export class FetchDataComponent {
  public users: User[] = [];

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    http.get<User[]>(baseUrl + 'main/demo').subscribe(result => {
      this.users = result;
    }, error => console.error(error));
  }
}

interface Game {
  name: string;
  appid: string;
  playtime: string;
  isRussian: boolean;
}

interface Group {
  name: string;
  members: number;
  description: string;
}

interface SummaryObj {
  profileLogs: string[];
  descriptionLogs: string[];
  groupLogs: string[];
  gameLogs: string[];
  friendLogs: string[];
  logs: string[];
  scannedRate: number;
}

interface User {
  steamId: string;
  steamId64: string;
  name: string;
  avatar: string;
  nationality: string;
  memberSince: string;
  summary: string;
  isPrivate: boolean;
  games: Game[];
  groups: Group[];
  friends: string[];
  summaryObj: SummaryObj;
  profileScore: number;
  summaryScore: number;
  gameScore: number;
  groupScore: number;
  friendScore: number;
}
