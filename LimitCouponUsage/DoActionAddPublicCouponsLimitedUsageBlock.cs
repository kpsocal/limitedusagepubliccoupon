using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Coupons;
using Sitecore.Commerce.Plugin.Promotions;
using Sitecore.Framework.Pipelines;

namespace KP.Sandbox.Pipelines.Blocks.Coupons
{
    [PipelineDisplayName("Coupons.block.DoActionAddPublicCouponsLimitedUsageBlock")]
    public class DoActionAddPublicCouponsLimitedUsageBlock : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>    
    {
        private readonly AddPublicCouponCommand _addPublicCouponCommand;
        private readonly IFindEntityPipeline _findEntityPipeline;
        private readonly IPersistEntityPipeline _persistEntityPipeline;

        public DoActionAddPublicCouponsLimitedUsageBlock(AddPublicCouponCommand addPublicCouponCommand, IFindEntityPipeline findEntityPipeline, IPersistEntityPipeline persistEntityPipeline)
          : base(null)
        {
            _addPublicCouponCommand = addPublicCouponCommand;
            _findEntityPipeline = findEntityPipeline;
            _persistEntityPipeline = persistEntityPipeline;
        }

        public override async Task<EntityView> Run(EntityView entityView, CommercePipelineExecutionContext context)
        {
            int limitValue = -999;
            string strCode = string.Empty;
            string regClassCode = string.Empty;
                        
            EntityView entityView1 = entityView;
            if (string.IsNullOrEmpty(entityView1 != null ? entityView1.Action : null) || !entityView.Action.Equals(context.GetPolicy<KnownCouponActionsPolicy>().AddPublicCoupon, StringComparison.OrdinalIgnoreCase) || (!entityView.Name.Equals(context.GetPolicy<KnownCouponViewsPolicy>().PublicCoupons, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(entityView.EntityId)))
                return entityView;

            Promotion promotion = context.CommerceContext.GetObject<Promotion>(p => p.Id.Equals(entityView.EntityId, StringComparison.OrdinalIgnoreCase));
            if (promotion == null)
                return entityView;

            ViewProperty code = entityView.Properties.FirstOrDefault(p => p.Name.Equals("Code", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(code != null ? code.Value : null))
            {
                strCode = code == null ? "Code" : code.DisplayName;
                string result = await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "InvalidOrMissingPropertyValue", new object[1]
                {
                    strCode
                }, "Invalid or missing value for property 'Code'.");
                return entityView;
            }
                        
            ViewProperty limitViewProperty = entityView.Properties.FirstOrDefault(p => p.Name.Equals("Coupon Usage Limit", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(limitViewProperty != null ? limitViewProperty.Value : null))
            {
                string errorCode = limitViewProperty == null ? "Coupon Usage Limit" : limitViewProperty.DisplayName;
                string result = await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "InvalidOrMissingPropertyValue", new object[1]
                {
                    errorCode
                }, "Invalid or missing value for property 'Code'.");
                return entityView;
            }

            if (limitViewProperty.Value != null)
            {
                bool isSuccess = int.TryParse(limitViewProperty.Value, out limitValue);
                if (!isSuccess)
                {
                    string errorCode = code == null ? "Coupon Usage Limit" : limitViewProperty.DisplayName;
                    string result = await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "InvalidOrMissingPropertyValue", new object[1]
                    {
                    errorCode
                    }, "Invalid or missing value for property 'Coupon Usage Limit'.");
                    return entityView;
                }
            }

            CommerceContext commerceContext = context.CommerceContext;


            string couponCode = code != null ? code.Value : null;            

            Promotion promotion2 = await _addPublicCouponCommand.Process(commerceContext, promotion, couponCode);

            string couponId = $"{CommerceEntity.IdPrefix<Coupon>()}{couponCode}";

            var couponData = await _findEntityPipeline.Run(new FindEntityArgument(typeof(Coupon), couponId, false), context);

            if (couponData != null && couponData is Coupon)
            {
                var coupon = couponData as Coupon;
                if (coupon != null)
                {
                    coupon.Policies = new List<Policy>()
                        {
                          new LimitUsagesPolicy()
                          {
                            LimitCount = limitValue
                          }
                        };                    

                    PersistEntityArgument persistEntityArgument = await _persistEntityPipeline.Run(new PersistEntityArgument((CommerceEntity)coupon), context);
                }
            }

            return entityView;

        }
    }
}
