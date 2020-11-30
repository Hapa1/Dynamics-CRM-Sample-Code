//Namespace to avoid conflicts with other functions
var Sdk = window.Sdk || {};
(

    /*
     * Autopopulates the contact quickview form based off of the value in initial contact lookupfield
     */
    function () {
        this.fieldOnUpdate = executionContext => {

            //Retrieves logical name of the contact lookup field from the quickview form
            const fieldLogicalName = executionContext.getEventSource()._attributeName;

            //Object with methods and information related to the quickview form
            const formContext = executionContext.getFormContext();

            //Checks if the contact lookup field is changed by checking if the context includes 
            if (formContext.getAttribute(fieldLogicalName).getValue()) {

                //Sets the current value of the contact lookup field from the context as a JS object
                const fieldValue = formContext.getAttribute(fieldLogicalName).getValue()[0];

                //Retrieves contact information based off of the entityType and id data we have in fieldValue
                Xrm.WebApi.retrieveRecord(fieldValue.entityType, fieldValue.id).then(
                    function success(result) {

                        //Set the form fields to the field values supplied by retrieveRecord
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