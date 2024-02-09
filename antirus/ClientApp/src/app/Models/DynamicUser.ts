import { SummaryObj } from './SummaryObj';

import { HttpClient } from '@angular/common/http';
import { Inject } from '@angular/core';
import { Game } from './Game';
import { Group } from './Group';
import { User } from './User';


export class DynamicUser implements User {
  //make getters for games, groups, friends that call the API
  private loadedGames: boolean = false;
  private loadedGroups: boolean = false;
  private loadedFriends: boolean = false;
  private http: HttpClient;
  private baseUrl: string;
  private user: User = {} as User;

  constructor(id64: string, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.http = http;
    this.baseUrl = baseUrl;
    this.user.steamId64 = id64;
    this.http.post<User>(this.baseUrl + 'api/Main/' + this.steamId64, {
      "games": true,
      "friends": false,
      "groups": true
    }).subscribe({
      next: (result) => {
        this.user = result;
      },
      error: (error) => {
        console.error(error);
        alert("Error: " + error.error);
      }
    });
  }
  //available getters from User
  public get steamId (): string | undefined { return this.user.steamId; }
  public get steamId64(): string { return this.user.steamId64; }
  public get name(): string | undefined { return this.user.name; }
  public get avatar(): string | undefined { return this.user.avatar; }
  public get nationality(): string | undefined { return this.user.nationality; }
  public get memberSince(): string | undefined { return this.user.memberSince; }
  public get summary(): string | undefined { return this.user.summary; }
  public get isPrivate(): boolean | undefined { return this.user.isPrivate; }
  public get groups(): Group[] | undefined { return this.user.groups;} //always available
  public friends: User[] | undefined;
  public get summaryObj(): SummaryObj | undefined { return this.user.summaryObj; }
  public get profileScore(): number | undefined { return this.user.profileScore; }
  public get summaryScore(): number | undefined { return this.user.summaryScore; }
  public get gameScore(): number | undefined { return this.user.gameScore; }
  public get groupScore(): number | undefined { return this.user.groupScore; }
  public get friendScore(): number | undefined { return this.user.friendScore; }

  //dynamic getters for games, groups, friends arrays
  public get games(): Game[] | undefined {
    if (!this.loadedGames) {
      this.loadedGames = true;
      this.http.get<Game[]>(this.baseUrl + 'api/Main/' + this.user.steamId64 + '/games').subscribe({
        next: (result) => {
          this.user.games = result;
        },
        error: (error) => {
          console.error(error);
          alert("Error: " + error.error);
        }
      });
    }
    return this.user.games;
  }

  public get ruGames(): Game[] | undefined {
    return this.user.games?.filter((game) => game.isRussian);
  }
  public get enGames(): Game[] | undefined {
    return this.user.games?.filter((game) => !game.isRussian);
  }
  
}
