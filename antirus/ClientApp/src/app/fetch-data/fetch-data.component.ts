import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/internal/Observable';
import { NestedTreeControl, TreeControl } from '@angular/cdk/tree';
import {CollectionViewer, SelectionChange, DataSource} from '@angular/cdk/collections';
import {FlatTreeControl} from '@angular/cdk/tree';
import {Injectable} from '@angular/core';
import {BehaviorSubject, merge} from 'rxjs';
import {map} from 'rxjs/operators';
import {MatProgressBarModule} from '@angular/material/progress-bar';
import {MatIconModule} from '@angular/material/icon';
import {MatButtonModule} from '@angular/material/button';
import {MatTreeModule} from '@angular/material/tree';
import { User } from '../Models/User';
import { DynamicUser } from '../Models/DynamicUser';

/** Flat node with expandable and level information */
export class DynamicFlatNode {
  constructor(
    public item: string,
    public level = 1,
    public expandable = false,
    public isLoading = false,
  ) {}
}

//database with actual REST API calls
// List<User> - show as tree
//      User - a quick summary
//              Email 
//              Name
//              Phone
//              other info via Object.keys

@Injectable({providedIn: 'root'})
export class RestDatabase {
  data : DynamicUser | undefined;
  rootLevelNodes: string[] = ['Friends'];
  http: HttpClient | undefined;
  baseUrl: string | undefined;

  /** Initial data from database */
  initialData(): DynamicFlatNode[] {
    return this.rootLevelNodes.map(name => new DynamicFlatNode(name, 0, true));
  }
  init(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.http = http;
    this.baseUrl = baseUrl;
  }

  getChildren(node: string): string[] | undefined {
    //get User as object and return keys or a value if it's a key
    if (node == "Friends") {
      if (!this.data) {
        if(!this.http || !this.baseUrl) throw new Error("Database not initialized");
        this.data = new DynamicUser("76561198045920783", this.http, this.baseUrl);
      }

    }

    //check if node is a key
    if (this.data && this.data.hasOwnProperty(node)) {
      //emulate this.data[node], cast User to any to access properties
      let value = (this.data as any)[node];
      if (typeof value== "object") {
        return Object.keys(value);
      }
      else {
        return [value as string];
      }
    }
    //if not then return keys as shown in interface
    else if (this.data) {
      return Object.keys(this.data);
    }
    else {
      return undefined;
    }
  }

  isExpandable(node: string): boolean {
    if(this.data === undefined) return false;
    return this.getChildren(node) !== undefined;
  }
}
/**
 * File database, it can build a tree structured Json object from string.
 * Each node in Json object represents a file or a directory. For a file, it has filename and type.
 * For a directory, it has filename and children (a list of files or directories).
 * The input will be a json object string, and the output is a list of `FileNode` with nested
 * structure.
 */
export class DynamicDataSource implements DataSource<DynamicFlatNode> {
  dataChange = new BehaviorSubject<DynamicFlatNode[]>([]);

  get data(): DynamicFlatNode[] {
    return this.dataChange.value;
  }
  set data(value: DynamicFlatNode[]) {
    this._treeControl.dataNodes = value;
    this.dataChange.next(value);
  }

  constructor(
    private _treeControl: FlatTreeControl<DynamicFlatNode>,
    private _database: RestDatabase,
  ) {}

  connect(collectionViewer: CollectionViewer): Observable<DynamicFlatNode[]> {
    this._treeControl.expansionModel.changed.subscribe(change => {
      if (
        (change as SelectionChange<DynamicFlatNode>).added ||
        (change as SelectionChange<DynamicFlatNode>).removed
      ) {
        this.handleTreeControl(change as SelectionChange<DynamicFlatNode>);
      }
    });

    return merge(collectionViewer.viewChange, this.dataChange).pipe(map(() => this.data));
  }

  disconnect(collectionViewer: CollectionViewer): void {}

  /** Handle expand/collapse behaviors */
  handleTreeControl(change: SelectionChange<DynamicFlatNode>) {
    if (change.added) {
      change.added.forEach(node => this.toggleNode(node, true));
    }
    if (change.removed) {
      change.removed
        .slice()
        .reverse()
        .forEach(node => this.toggleNode(node, false));
    }
  }

  /**
   * Toggle the node, remove from display list
   */
  toggleNode(node: DynamicFlatNode, expand: boolean) {
    const children = this._database.getChildren(node.item);
    const index = this.data.indexOf(node);
    if (!children || index < 0) {
      // If no children, or cannot find the node, no op
      return;
    }

    node.isLoading = true;

    setTimeout(() => {
      if (expand) {
        const nodes = children.map(
          name => new DynamicFlatNode(name, node.level + 1, this._database.isExpandable(name)),
        );
        this.data.splice(index + 1, 0, ...nodes);
      } else {
        let count = 0;
        for (
          let i = index + 1;
          i < this.data.length && this.data[i].level > node.level;
          i++, count++
        ) {}
        this.data.splice(index + 1, count);
      }

      // notify the change
      this.dataChange.next(this.data);
      node.isLoading = false;
    }, 1000);
  }
}

/**
 * @title Tree with dynamic data
 */


@Component({
  selector: 'app-fetch-data',
  templateUrl: './fetch-data.component.html',
  styleUrls: ['./fetch-data.component.scss'],
})
export class FetchDataComponent {


  treeControl: FlatTreeControl<DynamicFlatNode>;

  dataSource: DynamicDataSource;

  getLevel = (node: DynamicFlatNode) => node.level;

  isExpandable = (node: DynamicFlatNode) => node.expandable;

  hasChild = (_: number, _nodeData: DynamicFlatNode) => _nodeData.expandable;

  public showEnGames: boolean = false;

  public user!: DynamicUser | null;
  public steamId: string;
  public loading: boolean = false;

  private baseUrl: string;
  private http: HttpClient;

  constructor(database: RestDatabase, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    this.treeControl = new FlatTreeControl<DynamicFlatNode>(this.getLevel, this.isExpandable);
    this.dataSource = new DynamicDataSource(this.treeControl, database);
    database.init(http, baseUrl);

    this.dataSource.data = database.initialData();

    this.baseUrl = baseUrl;
    this.http = http;
    this.steamId = "gameplayer55055";

  }
  public scan() {
    if (this.loading) return;
    this.loading = true;
    this.user = new DynamicUser(this.steamId, this.http, this.baseUrl);
    this.loading = false;
  }
}
