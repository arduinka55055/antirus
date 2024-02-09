import { Component, Input } from '@angular/core';
import { Group } from '../Models/Group';

@Component({
  selector: 'app-groupview',
  templateUrl: './groupview.component.html',
  styleUrls: ['./groupview.component.scss']
})
export class GroupviewComponent {

  constructor() { }

  ngOnInit(): void {
  }

  @Input()
  public group!: Group;
  
  public get grpSteamLink(): string {
    return "https://steamcommunity.com/gid/" + this.group.id;
  }

  public get getNameAll(): string {
    return this.group.isRussian ? this.group.name + " [RU]" : this.group.name;
  }
}
