import { App } from "@web-atoms/core/dist/App";
import { AtomList } from "@web-atoms/core/dist/core/AtomList";
import { INameValuePairs } from "@web-atoms/core/dist/core/types";
import MenuService from "../services/MenuService";

export default class MenuItem {

    public group: string;

    public enabled: boolean;

    public expand: boolean = false;

    public isGroup: boolean = false;

    public label: string;

    public icon: string;

    public children: AtomList<MenuItem>;

    public action: (menuItem: MenuItem) => any;

    constructor(
        public readonly app: App,
        public readonly menuService: MenuService
    ) {
        this.enabled = true;
        this.children = new AtomList();
    }

    public click(): any {
        return this.action(this);
    }

    public add(label: string, action: () => any, icon?: string): MenuItem {
        const m = this.menuService.create(label, action, icon);
        this.children.add(m);
        return m;
    }

    public addGroup(label: string, icon?: string): MenuItem {
        const m = this.menuService.createGroup(label, icon);
        this.children.add(m);
        return m;
    }

    public addLink(label: string, pageSrc: string | any, pageParameters?: INameValuePairs, icon?: string): MenuItem {
        const m = this.menuService.createLink(label, pageSrc, pageParameters, icon);
        this.children.add(m);
        return m;
    }

    public addTabLink(
        label: string,
        pageSrc: string | any,
        pageParameters?: INameValuePairs,
        icon?: string): MenuItem {
        const m = this.menuService.createLink(label, pageSrc, pageParameters, icon, { target: "app" });
        this.children.add(m);
        return m;
    }
}
