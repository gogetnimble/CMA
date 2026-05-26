import { expect } from "chai";
import { Hsl } from "./mockHslExecutionContext.js";
import { createMockExecutionContext } from "./mockExecutionContext.js";
import { CMAConnectForm } from "../src/Account.js";  // Import your form class directly

global.Hsl = Hsl;

describe("Account CMAConnect Form Tests", () => {
  let ctx;
  let form;

  beforeEach(() => {
    ctx = createMockExecutionContext("account", {
      name: "Old Account Name",
      telephone1: "12345",
      fax: "67890"
    });

    const formContext = ctx.getFormContext();
    form = new CMAConnectForm(formContext);
    form.initialize();
  });

  it("should initialize the CMAConnect form", () => {
    expect(form).to.be.instanceOf(CMAConnectForm);
  });

  it("should trigger name onchange", () => {
    const nameAttr = ctx.getFormContext().getAttribute("name");
    nameAttr.setValue("New Account Name");
    nameAttr.fireOnChange();
    // You can assert expected logs or other side effects here
  });

  it("should trigger phone onchange", () => {
    const phoneAttr = ctx.getFormContext().getAttribute("telephone1");
    phoneAttr.setValue("6133254463");
    phoneAttr.fireOnChange();
    // You can assert expected handling, e.g., value reset if numeric validation runs
    expect(phoneAttr.getValue()).to.equal("(613) 325-4463"); 
  });
});
