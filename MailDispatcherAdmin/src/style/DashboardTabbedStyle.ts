import Colors from "@web-atoms/core/dist/core/Colors";
import { AtomTabbedPageStyle } from "@web-atoms/core/dist/web/styles/AtomTabbedPageStyle";
import { IStyleDeclaration } from "@web-atoms/core/dist/web/styles/IStyleDeclaration";
import FontAwesomeSolid from "@web-atoms/font-awesome/dist/FontAwesomeSolid";
export class DashboardTabbedStyle extends AtomTabbedPageStyle {

    public get root(): IStyleDeclaration {
        const baseStyle = this.getBaseProperty(DashboardTabbedStyle, "root");
        return {
            ... baseStyle,
            subclasses: {
                ... baseStyle.subclasses,
                " .tab-view-style": this.tabViewStyle,
                " .tab-item": this.tabItem,
                " .selected-tab-item": this.selectedTabItem,
                " .close-button": this.closeButton
            }
        };
    }

    public get tabViewStyle(): IStyleDeclaration {
        return {
            position: "absolute",
            bottom: "0px",
            right: "0px",
            top: "20px",
            left: "0px"
        };
    }
    public get tabItem(): IStyleDeclaration {
        return {
        // tslint:disable-next-line:no-string-literal
            ... this.getBaseProperty(DashboardTabbedStyle, "tabItem"),
            backgroundColor: "#009e76 !important",
            border: "none",
            marginTop: "2px",
            paddingBottom: "3px",
            color: "#d1fff3",
            subclasses: {
                ":hover": {
                    backgroundColor: "#007558 !important"
                },
                "> div": {
                    display: "inline-block",
                    padding: (this.padding || this.theme.padding) + "px",
                    paddingRight: ((this.padding || this.theme.padding) + 23) + "px",
                    right: "22px"
                }
            }
        };
    }

    public get selectedTabItem(): IStyleDeclaration {
        return {
        // tslint:disable-next-line:no-string-literal
            ... this.getBaseProperty(DashboardTabbedStyle, "selectedTabItem"),
            backgroundColor: "White !important",
            color: "#2e2e2e",
            subclasses: {
                ":hover": {
                    backgroundColor: "#eee",
                    color: "#1e1e1e"
                },
                "> div": {
                    display: "inline-block",
                    padding: (this.padding || this.theme.padding) + "px",
                    paddingRight: ((this.padding || this.theme.padding) + 23) + "px",
                    right: "22px"
                }
            }
        };
    }

    public get closeButton(): IStyleDeclaration {
        return {
            ... this.getBaseProperty(DashboardTabbedStyle, "closeButton"),
            fontFamily: "Font Awesome 5 Free",
            fontWeight: 900,
            color: Colors.black,
            content: FontAwesomeSolid.windowClose,
            subclasses: {
                ":hover": {
                    color: Colors.red
                }
            }
        };
    }
}
