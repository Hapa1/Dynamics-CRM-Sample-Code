var Sdk = window.Sdk || {};
(
    function () {
        this.fieldOnUpdate = executionContext => {
            alert("changed")
            const fieldLogicalName = executionContext.getEventSource()._attributeName;
            const formContext = executionContext.getFormContext();
            if (formContext.getAttribute(fieldLogicalName).getValue()) {
                const fieldValue = formContext.getAttribute(fieldLogicalName).getValue()[0];
                Xrm.WebApi.retrieveRecord(fieldValue.entityType, fieldValue.id).then(
                    function success(result) {
                        formContext.getAttribute("firstname").setValue(result.crf92_srfnamfirstname);
                        formContext.getAttribute("lastname").setValue(result.crf92_srlnamlastname);
                        formContext.getAttribute("address1_line1").setValue(result.crf92_srcad1consumeraddress1);
                        formContext.getAttribute("address1_line2").setValue(result.crf92_srcad2consumeraddress2);
                        formContext.getAttribute("address1_city").setValue(result.crf92_consumercity);
                        formContext.getAttribute("address1_postalcode").setValue(result.crf92_srczipconsumerzip);
                    },
                    function (error) {
                        console.log(error.message);
                    }
                )
            }
        }
    }
).call(Sdk)