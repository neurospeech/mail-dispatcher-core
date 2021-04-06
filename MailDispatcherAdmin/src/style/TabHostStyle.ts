import { AtomStyle } from "@web-atoms/core/dist/web/styles/AtomStyle";
import { IStyleDeclaration } from "@web-atoms/core/dist/web/styles/IStyleDeclaration";

export class TabHostStyle extends AtomStyle {

    public get root(): IStyleDeclaration {
        return {
            position: "absolute",
            left: "0",
            top: "0",
            right: "0",
            bottom: "0",
            color: "#3c4144",
            fontSize: "14px",
            fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen-Sans, " +
            "Ubuntu, Cantarell, 'Helvetica Neue', sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol'",
            subclasses: {
                ".menu-view": this.menuView,
                " .heading-title": {
                    color: "rgb(209, 255, 243)",
                    backgroundColor: "#1aae88",
                    fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen-Sans, " +
            "Ubuntu, Cantarell, 'Helvetica Neue', sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol'",
                    padding: "5px 5px 5px 30px",
                    fontWeight: "700",
                    fontSize: "20px",
                    height: "100%"
                },
                " .header2": {
                    backgroundColor: "#27d4a8",
                    height: "100%"
                }
            }
        };
    }

    public get menuView(): IStyleDeclaration {
        return {
            borderRight: "1px solid #eee",
                    height: "100%",
                    paddingTop: "10px"
        };
    }

    public get tabbedPage(): IStyleDeclaration {
        return {
            top: "10px !important"
        };
    }
}
