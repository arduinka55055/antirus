import { SummaryObj } from './SummaryObj';
import { Group } from './Group';
import { Game } from './Game';


export interface User {
  steamId: string | undefined;
  steamId64: string;
  name: string | undefined;
  avatar: string | undefined;
  nationality: string | undefined;
  memberSince: string | undefined;
  summary: string | undefined;
  isPrivate: boolean | undefined;
  games: Game[] | undefined;
  groups: Group[] | undefined;
  friends: User[] | undefined;
  summaryObj: SummaryObj | undefined;
  profileScore: number | undefined;
  summaryScore: number | undefined;
  gameScore: number | undefined;
  groupScore: number | undefined;
  friendScore: number | undefined;
}
