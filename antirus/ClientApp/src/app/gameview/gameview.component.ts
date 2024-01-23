import { Component, Input } from '@angular/core';
import { Game } from '../Models/Game';

@Component({
  selector: 'app-gameview',
  templateUrl: './gameview.component.html',
  styleUrls: ['./gameview.component.scss'],
  //game input
  inputs: ['game']
})
//game view component that takes in a Game model and displays it
export class GameviewComponent {
  constructor() { }

  ngOnInit(): void {
  }

  @Input()
  public game!: Game;
  
  public get gameStoreLink(): string {
    return "https://store.steampowered.com/app/" + this.game.appid;
  }

  public get getNameAll(): string {
    return this.game.isRussian ? this.game.name + " [RU]" : this.game.name;
  }
}
