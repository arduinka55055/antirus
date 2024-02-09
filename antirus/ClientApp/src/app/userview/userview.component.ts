import { Component, Input } from '@angular/core';
import { User } from '../Models/User';

@Component({
  selector: 'app-userview',
  templateUrl: './userview.component.html',
  styleUrls: ['./userview.component.scss'],
  inputs: ['user']
})
export class UserviewComponent {

  constructor() { }

  ngOnInit(): void {
  }

  @Input()
  public user!: User;
  
}
