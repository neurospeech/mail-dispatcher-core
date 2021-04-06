import Bind from "@web-atoms/core/dist/core/Bind";
import {BindableProperty} from "@web-atoms/core/dist/core/BindableProperty";
import XNode from "@web-atoms/core/dist/core/XNode";
import {AtomControl} from "@web-atoms/core/dist/web/controls/AtomControl";
import {AtomItemsControl} from "@web-atoms/core/dist/web/controls/AtomItemsControl";
import MenuItemStyle from "../style/MenuItemStyle";

export default class MenuItemTemplate extends AtomControl {

	constructor(app: any, e?: any) {
		super(app, e || document.createElement("div"));
	}

	public create(): void {
		this.defaultControlStyle = MenuItemStyle;

		this.render(
		<div
			styleClass={Bind.oneWay((x) => ({
        [this.controlStyle.name]: 1,
        group: x.data.isGroup
    }))}
			eventClick={Bind.event((x) => this.data.click())}>
			<div>
				<i
					class={Bind.oneTime((x) => ({
                icon: 1,
                [x.data.icon]: 1
            }))}>
				</i>
				<span
					class="title"
					text={Bind.oneWay((x) => x.data.label)}>
				</span>
				<i
					class="group-icon fas fa-angle-down">
				</i>
			</div>
			<AtomItemsControl
				styleDisplay={Bind.oneWay((x) => x.data.expand ? "" : "none")}
				items={Bind.oneWay((x) => x.data.expand ? x.data.children : [])}
				itemTemplate={Bind.oneTime(() => MenuItemTemplate)}>
			</AtomItemsControl>
		</div>
		);
	}
}
