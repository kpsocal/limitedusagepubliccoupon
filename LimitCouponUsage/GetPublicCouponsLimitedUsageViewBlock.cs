using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Coupons;
using Sitecore.Commerce.Plugin.Promotions;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;

namespace KP.Sandbox.Pipelines.Blocks.Coupons
{
    [PipelineDisplayName("Coupons.block.GetPublicCouponsLimitedUsageViewBlock")]
    public class GetPublicCouponsLimitedUsageViewBlock : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>
    {
        private readonly IFindEntitiesInListPipeline _findEntitiesInListPipeline;
        public GetPublicCouponsLimitedUsageViewBlock(IFindEntitiesInListPipeline findEntitiesInListPipeline)
        : base(null)
        {
            _findEntitiesInListPipeline = findEntitiesInListPipeline;
        }
                
        public override async Task<EntityView> Run(EntityView entityView, CommercePipelineExecutionContext context)
        {
            Condition.Requires(entityView).IsNotNull(string.Format("{0}: The argument cannot be null", Name));
            EntityViewArgument entityViewArgument = context.CommerceContext.GetObject<EntityViewArgument>();

            if (string.IsNullOrEmpty(entityViewArgument != null ? entityViewArgument.ViewName : (string)null) ||
                !entityViewArgument.ViewName.Equals(context.GetPolicy<KnownCouponViewsPolicy>().PublicCoupons,
                    StringComparison.OrdinalIgnoreCase) && !entityViewArgument.ViewName.Equals(
                    context.GetPolicy<KnownPromotionsViewsPolicy>().Master, StringComparison.OrdinalIgnoreCase))
            {
                return entityView;
            }

            var forAction = entityViewArgument.ForAction;

            if (!string.IsNullOrEmpty(forAction) && forAction.Equals(context.GetPolicy<KnownCouponActionsPolicy>().AddPublicCoupon,
                    StringComparison.OrdinalIgnoreCase)
                    && entityViewArgument.ViewName.Equals(context.GetPolicy<KnownCouponViewsPolicy>().PublicCoupons, StringComparison.OrdinalIgnoreCase))
            {
                List<ViewProperty> properties = entityView.Properties;

                ViewProperty viewPropertyCode = new ViewProperty
                {
                    Name = "Code",
                    RawValue = string.Empty,
                    IsReadOnly = false
                };

                properties.Add(viewPropertyCode);
               

                ViewProperty viewPropertyUsageCount = new ViewProperty
                {
                    Name = "Coupon Usage Limit",
                    RawValue = string.Empty,
                    IsReadOnly = false,
                    IsRequired = false,
                    UiType = "Dropdown"
                };

                var limitUsageCountList = new List<Selection>();
                limitUsageCountList.Add(new Selection() { Name = "-999", IsDefault = true, DisplayName = "No Limit (Default)" });
                limitUsageCountList.Add(new Selection() { Name = "100", IsDefault = false, DisplayName = "Limit 100 Usages" });
                limitUsageCountList.Add(new Selection() { Name = "200", IsDefault = false, DisplayName = "Limit 200 Usages" });
                limitUsageCountList.Add(new Selection() { Name = "300", IsDefault = false, DisplayName = "Limit 300 Usages" });
                limitUsageCountList.Add(new Selection() { Name = "400", IsDefault = false, DisplayName = "Limit 400 Usages" });
                limitUsageCountList.Add(new Selection() { Name = "500", IsDefault = false, DisplayName = "Limit 500 Usages" });

                viewPropertyUsageCount.Policies = new List<Policy>()
                {
                   new AvailableSelectionsPolicy()
                  {
                    List =  limitUsageCountList
                  }
                };

                properties.Add(viewPropertyUsageCount);
                entityView.UiHint = "Flat";                            
                return entityView;
            }

            Promotion entity = entityViewArgument.Entity as Promotion;

            if (entity != null)
            {
                EntityView publicCouponsView;

                if (entityViewArgument.ViewName.Equals(context.GetPolicy<KnownPromotionsViewsPolicy>().Master, StringComparison.OrdinalIgnoreCase))
                {
                    EntityView entityView1 = new EntityView();
                    entityView1.EntityId = (entity != null ? entity.Id : (string)null) ?? string.Empty;
                    string publicCoupons = context.GetPolicy<KnownCouponViewsPolicy>().PublicCoupons;
                    entityView1.Name = publicCoupons;
                    publicCouponsView = entityView1;
                    entityView.ChildViews.Add(publicCouponsView);
                }
                else
                {
                    publicCouponsView = entityView;
                }

                publicCouponsView.UiHint = "Table";
                FindEntitiesInListArgument entitiesInListArgument = await _findEntitiesInListPipeline.Run(new FindEntitiesInListArgument(typeof(Coupon), string.Format(context.GetPolicy<KnownCouponsListsPolicy>().PublicCoupons, (object)entity.FriendlyId), 0, int.MaxValue), context);

                if (entitiesInListArgument != null)
                {
                    CommerceList<CommerceEntity> list = entitiesInListArgument.List;

                    if (list != null)
                        list.Items.ForEach((c =>
                        {
                            Coupon coupon = c as Coupon;
                            if (coupon == null)
                                return;
                            EntityView entityView1 = new EntityView();
                            entityView1.EntityId = entityView.EntityId;
                            entityView1.ItemId = coupon.Id;
                            string couponDetails = context.GetPolicy<KnownCouponViewsPolicy>().CouponDetails;
                            entityView1.Name = couponDetails;

                            EntityView entityViewCoupon = entityView1;
                            List<ViewProperty> properties1 = entityViewCoupon.Properties;
                            ViewProperty viewPropertyItemId = new ViewProperty();
                            viewPropertyItemId.Name = "ItemId";
                            viewPropertyItemId.RawValue = (coupon.Id ?? string.Empty);
                            viewPropertyItemId.IsReadOnly = true;
                            viewPropertyItemId.IsHidden = false;
                            properties1.Add(viewPropertyItemId);

                            List<ViewProperty> properties2 = entityViewCoupon.Properties;
                            ViewProperty viewPropertyCode = new ViewProperty();
                            viewPropertyCode.Name = "Code";
                            viewPropertyCode.RawValue = (coupon.Code ?? string.Empty);
                            viewPropertyCode.IsReadOnly = true;
                            properties2.Add(viewPropertyCode);


                            List<ViewProperty> propertiesUsage = entityViewCoupon.Properties;
                            ViewProperty propertiesUsageProperty = new ViewProperty();
                            propertiesUsageProperty.Name = "Coupon Usage Limit";
                            var limit = ((LimitUsagesPolicy)coupon.Policies.Where(x => x is LimitUsagesPolicy).FirstOrDefault()).LimitCount;
                            var stringLimit = limit.ToString();
                            propertiesUsageProperty.RawValue = stringLimit;
                            propertiesUsageProperty.IsReadOnly = true;
                            propertiesUsage.Add(propertiesUsageProperty);


                            publicCouponsView.ChildViews.Add(entityViewCoupon);

                        }));
                }
            }

            return entityView;
        }
    }
}
