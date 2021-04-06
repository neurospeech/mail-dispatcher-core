import { App } from "@web-atoms/core/dist/App";
import { AtomBinder } from "@web-atoms/core/dist/core/AtomBinder";
import { AtomList } from "@web-atoms/core/dist/core/AtomList";
import { BindableProperty } from "@web-atoms/core/dist/core/BindableProperty";
import { INameValuePairs } from "@web-atoms/core/dist/core/types";
import DISingleton from "@web-atoms/core/dist/di/DISingleton";
import { Inject } from "@web-atoms/core/dist/di/Inject";
import { IPageOptions, NavigationService } from "@web-atoms/core/dist/services/NavigationService";
import MenuItem from "../models/MenuItem";

@DISingleton()
export default class MenuService {

    @BindableProperty
    public menus: AtomList<MenuItem>;

    @BindableProperty
    public isOpen: boolean;

    public get groupedMenus() {
        const a = [];
        for (const iterator of this.menus) {
            const g = [] as any;
            g.key = iterator;
            a.push(g);
            for (const child of iterator.children) {
                g.push(child);
            }
        }
        return a;
    }

    constructor(
        @Inject public readonly app: App
    ) {
        this.menus = new AtomList();
    }

    public refresh() {
        AtomBinder.refreshValue(this, "groupedMenus");
    }

    public add(label: string, action: () => any, icon?: string): MenuItem {
        const m = this.create(label, action, icon);
        this.menus.add(m);
        return m;
    }

    public addGroup(label: string, icon?: string): MenuItem {
        const m = this.createGroup(label, icon);
        m.isGroup = true;
        this.menus.add(m);
        return m;
    }

    public addLink(label: string, pageSrc: string | any , pageParameters?: INameValuePairs, icon?: string): MenuItem {
        const m = this.createLink(label, pageSrc, pageParameters, icon);
        this.menus.add(m);
        return m;
    }

    public createLink(
        label: string,
        pageSrc: string | any,
        pageParameters?: INameValuePairs,
        icon?: string,
        options?: IPageOptions
    ): MenuItem {
        const nav: NavigationService = this.app.resolve(NavigationService);
        const p = pageParameters || {};
        p.title = p.title || label;
        const m = this.create(label, () => nav.openPage(pageSrc, p, options).catch((e) => {
            // tslint:disable-next-line: triple-equals
            if (e != "cancelled") {
                // tslint:disable-next-line: no-console
                console.error(e);
            }
        }), icon);
        return m;
    }

    public createGroup(label: string, icon?: string): MenuItem {
        return this.create(label, (m) => m.expand = !m.expand, icon);
    }

    public create(label: string, action: (m: MenuItem) => any, icon?: string): MenuItem {
        const menu = new MenuItem(this.app, this);
        menu.label = label;
        menu.action = () => {
            this.isOpen = false;
            return action(menu);
        };
        if (icon) {
            menu.icon = icon;
        }

        return menu;
    }

    public addTabLink(
        label: string,
        pageSrc: string | any,
        pageParameters?: INameValuePairs,
        icon?: string): MenuItem {
        const m = this.createLink(label, pageSrc, pageParameters, icon, { target: "app" });
        this.menus.add(m);
        return m;
    }

}
