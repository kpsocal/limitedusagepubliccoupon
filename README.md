# Limited Use Public Coupon

Add the following code in configure sitecore

                            .ConfigurePipeline<IGetEntityViewPipeline>(c =>
                             c.Replace<GetPublicCouponsViewBlock, GetPublicCouponsLimitedUsageViewBlock>())

                             .ConfigurePipeline<IDoActionPipeline>(c =>
                             c.Replace<DoActionAddPublicCouponBlock, DoActionAddPublicCouponsLimitedUsageBlock>())
