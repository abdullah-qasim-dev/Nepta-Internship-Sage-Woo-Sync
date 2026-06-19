using SageIntegration.Configuration;
using SageIntegration.SageRepository.Customer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SageRepo = SageIntegration.SageRepository;

namespace SageIntegration.SageService.Product
{
    public class SageToWoo: ISageToWoo
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<Worker> _logger;
        public SageToWoo(IServiceScopeFactory serviceScopeFactory, ILogger<Worker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }
        public async Task addSageToWooAsync()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var SageProductRepository = scope.ServiceProvider.GetRequiredService<SageRepo.Product.IProductRepository>();
                var WooproductRepository = scope.ServiceProvider.GetRequiredService<SageIntegration.WooRepository.Product.IProductRepository>();
                try
                {
                    // Fetch all customers
                    var products = await SageProductRepository.GetAll();
                    LogManager.Instance.LogMessage("Total Product from Sage: " + products.Count.ToString(), "Prodcut");

                    //Process or log the customers
                    foreach (var product in products)
                    {
                        try
                        {
                            var wooproduct = await WooproductRepository.GetProduct(product.ProductCode);
                            if (wooproduct == null)
                            {
                                await WooproductRepository.AddAsync(product);
                            }
                            else
                            {
                                await WooproductRepository.UpdateAsync(product, wooproduct);
                            }
                        }
                        catch (Exception ex)
                        {

                            LogManager.Instance.LogMessage("Error While Adding or Updating Product:" + product.ProductCode + "ERROR:" + ex.Message, "Customer");
                            LogManager.Instance.LogException(ex, "Product");
                        }

                    }



                }
                catch (Exception ex)
                {

                    LogManager.Instance.LogMessage("Error While Running Product Service ERROR:" + ex.Message, "Product");
                    LogManager.Instance.LogException(ex, "Product");
                    _logger.LogError(ex, "Error occurred while retrieving ppp.");
                }
            }
        }
    }
}
