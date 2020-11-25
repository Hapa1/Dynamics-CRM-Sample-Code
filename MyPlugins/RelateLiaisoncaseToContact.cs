using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace MyCRM
{
    public class RelateLiaisoncaseToContact : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {

            // Obtain the tracing service
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity liaisonCase = (Entity)context.InputParameters["Target"];

                // Obtain the organization service reference which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory =
                    (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {

                    // Check if crf92_initialcontact lookup field attribute is in the current context
                    if (liaisonCase.Attributes.Contains("crf92_initialcontact"))
                    {

                        // Gets the name of the many to many relationship between contact and liaison case
                        string relationshipName = GetManyToManyRelationshipMetadata("contact", liaisonCase.LogicalName, service);
                        Relationship relationship = new Relationship(relationshipName);

                        // Check if he many to many relationship exists
                        if (relationshipName != null)
                        {

                            // Stores the old value of crf92_initialcontact using a registerd on the Plugin Regisration Tool
                            Entity preMessageImage = context.PreEntityImages["image"];

                            // If there is an old value in crf92_initialcontact, delete the relation between the current context and old contact
                            if (preMessageImage.Attributes.Contains("crf92_initialcontact"))
                            {
                                EntityReference contactPreImage = (EntityReference)preMessageImage.Attributes["crf92_initialcontact"];
                                Dissasociate(liaisonCase, relationship, contactPreImage, service);
                            }

                            // If there is a new nonnull value in crf92_initialcontact, create a relation between the current context and new contact
                            if (liaisonCase.Attributes["crf92_initialcontact"] != null)
                            {
                                EntityReference contactModified = (EntityReference)liaisonCase.Attributes["crf92_initialcontact"];
                                Associate(liaisonCase, relationship, contactModified, service);
                            }
                        }
                    }
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in the RelateLiaisoncaseToContact plugin.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("RelateLiaisoncaseToContact: {0}", ex.ToString());
                    throw;
                }
            }
        }

        /// <summary>  
        ///  Creates a link between a context and target record
        /// </summary> 
        /// <param name="contextEntity">Entity object representing the representing the record that we wish to relate</param>
        /// <param name="relationship">Relationship object representing the relationship between the context and target entity</param>
        /// <param name="targetEntity">EntityReference object representing the record that we wish to relate to</param>
        /// <param name="service">IOrganizationService object that lets us manipulate CDS metadata</param>
        public static void Associate(Entity contextEntity, Relationship relationship, EntityReference targetEntity, IOrganizationService service)
        {
            EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
            relatedEntities.Add(targetEntity);

            service.Associate(contextEntity.LogicalName, contextEntity.Id, relationship, relatedEntities);
        }

        /// <summary>  
        ///  Deletes a link between a context and target record
        /// </summary> 
        /// <param name="contextEntity">Entity object representing the representing the record that we wish to relate</param>
        /// <param name="relationship">Relationship object representing the relationship between the context and target entity</param>
        /// <param name="targetEntity">EntityReference object representing the record that we wish to relate to</param>
        /// <param name="service">IOrganizationService object that lets us retrieve and manipulate CDS metadata</param>
        public static void Dissasociate(Entity contextEntity, Relationship relationship, EntityReference targetEntity, IOrganizationService service)
        {
            EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
            relatedEntities.Add(targetEntity);

            service.Disassociate(contextEntity.LogicalName, contextEntity.Id, relationship, relatedEntities);
        }

        /// <summary>  
        /// Gets logical name of the many to many relation between 2 entities
        /// </summary> 
        /// <param name="entity1LogicalName">Logical name of the first data entity</param>
        /// <param name="entity2LogicalName">Logical name of the second data entity</param>
        /// <param name="service">IOrganizationService object that lets us retrieve and manipulate CDS metadata</param>
        /// <returns>
        /// Logical name of a many to many relation as a string. Null if no many to many relation exists 
        /// </returns>
        public static String GetManyToManyRelationshipMetadata(string entity1LogicalName, string entity2LogicalName, IOrganizationService service)
        {
            //String array to store logical names provided in the parameters. Sort so we can compare it later
            string[] entityLogicalNamesInput = new string[2] { entity1LogicalName, entity2LogicalName };
            Array.Sort(entityLogicalNamesInput);

            //String array that will store logical names fetched from many to many relationship metadata
            string[] entityLogicalNames = new string[2];

            //Creates a new RetrieveEntityRequest object to fetch relationship metadata given the logical name of a data entity
            RetrieveEntityRequest req = new RetrieveEntityRequest()
            {
                EntityFilters = EntityFilters.Relationships,
                LogicalName = entity1LogicalName
            };

            //Executes the request message we just created and stores it as a RetrieveEntityResponse object
            RetrieveEntityResponse res = (RetrieveEntityResponse)service.Execute(req);

            //Iterate through all the many to many relationships found in the metadata
            foreach (ManyToManyRelationshipMetadata a in res.EntityMetadata.ManyToManyRelationships)
            {
                //Store logical names as elements in the array and sort for comparison
                entityLogicalNames[0] = a.Entity1LogicalName;
                entityLogicalNames[1] = a.Entity2LogicalName;
                Array.Sort(entityLogicalNames);

                //Compare the arrays to see if the logical names in the arrays match.
                if (entityLogicalNamesInput.SequenceEqual(entityLogicalNames))
                {
                    //Return the logical name of the many to many relationship
                    return a.Entity1NavigationPropertyName;
                }
            }

            //No many to many relationship found. Return null
            return null;
        }

    }
}
