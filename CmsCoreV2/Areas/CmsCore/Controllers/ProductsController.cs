using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CmsCoreV2.Data;
using CmsCoreV2.Models;
using Microsoft.AspNetCore.Authorization;
using SaasKit.Multitenancy;
using Z.EntityFramework.Plus;

namespace CmsCoreV2.Areas.CmsCore.Controllers
{
    [Authorize(Roles = "ADMIN,PRODUCT,Supplier")]
    [Area("CmsCore")]
    public class ProductsController : ControllerBase
    {
 
        public ProductsController(ApplicationDbContext context, ITenant<AppTenant> tenant) : base(context, tenant)
        {
                
        }
        [Authorize(Roles="ADMIN,ProductIndex")]
        // GET: CmsCore/Products
        public async Task<IActionResult> Index(long? supplierId)
        {
            if (User.IsInRole("Supplier")) {
                supplierId = _context.Suppliers.FirstOrDefault(s=>s.UserName.ToLower() == User.Identity.Name.ToLower())?.Id;
            }
            var applicationDbContext = _context.Products.Include(p=>p.ProductProductCategories).ThenInclude(t=>t.ProductCategory).Include(p => p.Language).Where(w=>(supplierId.HasValue?w.SupplierId==supplierId:true));
            ViewBag.Suppliers = new SelectList(_context.Suppliers.ToList(),"Id","Name",supplierId);
            return View(await applicationDbContext.ToListAsync());
        }
        [Authorize(Roles="ADMIN,ProductDetails")]
        // GET: CmsCore/Products/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            long? supplierId = null;
            if (User.IsInRole("Supplier")) {
                supplierId = _context.Suppliers.FirstOrDefault(s=>s.UserName.ToLower() == User.Identity.Name.ToLower())?.Id;
            }
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Language)
                .SingleOrDefaultAsync(m => m.Id == id && (supplierId.HasValue?m.SupplierId==supplierId:true));
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
        public IList<AttributeItem> GetAttributeItems(long attributeId) {
            return _context.AttributeItems.Where(a=>a.AttributeId == attributeId).ToList();
        }
        [Authorize(Roles="ADMIN,ProductCreate")]
        // GET: CmsCore/Products/Create
        public IActionResult Create()
        {
            var product = new Product();
            product.CreatedBy = User.Identity.Name ?? "username";
            product.CreateDate = DateTime.Now;
            product.UpdatedBy = User.Identity.Name ?? "username";
            product.UpdateDate = DateTime.Now;
            product.AppTenantId = tenant.AppTenantId;
            ViewData["LanguageId"] = new SelectList(_context.Languages, "Id", "Culture");
            ViewBag.CategoryList = GetProductCategories();
            long? supplierId = null;
            if (User.IsInRole("Supplier")) {
                supplierId = _context.Suppliers.FirstOrDefault(s=>s.UserName.ToLower() == User.Identity.Name.ToLower())?.Id;
            }
            ViewBag.ShippingClasses = new SelectList(_context.ShippingClasses,"Id","ShippingClassName",product.ShippingClassId);
            ViewBag.Suppliers = new SelectList(_context.Suppliers,"Id","Name",supplierId);
            ViewBag.Attributes = new SelectList(_context.Attributes,"Id","Name");
            ViewBag.ShippingCities = new SelectList(_context.Regions.Where(r => r.RegionType == RegionType.City).ToList(),"Id","Name");
            ViewBag.ShippingZones = new SelectList(_context.ShippingZones.ToList(),"Id","Name");
            return View(product);
        }
        [Authorize(Roles="ADMIN,ProductCreate")]
        // POST: CmsCore/Products/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ShippingClassId,CatalogVisibility,SupplierId,AdditionalInfo,IsNew,IsApproved,Name,Slug,Description,LanguageId,UnitPrice,SalePrice,TaxStatus,TaxClass,StockCode,StockCount,StockStatus,ShippingCityId,ShippingZoneId,ShippingMethod,ProductType,ProductUrl,PurchaseNote,MenuOrder,ProductImage,ShortDescription,IsFeatured,Id,CreateDate,CreatedBy,UpdateDate,UpdatedBy,AppTenantId")] Product product, string categoriesHidden,long[] AttributeItemId, string[] Media, float[] ShippingPrice)
        {
            long? supplierId = null;
            if (User.IsInRole("Supplier")) {
                supplierId = _context.Suppliers.FirstOrDefault(s=>s.UserName.ToLower() == User.Identity.Name.ToLower())?.Id;
            }
            if (ModelState.IsValid)
            {
                product.SupplierId=supplierId;
                _context.Add(product);
                await _context.SaveChangesAsync();
                UpdateProductProductCategories(product.Id, categoriesHidden);
                var prd = _context.Products.Include(i=>i.ShippingPrices).Include(i=>i.ProductMedias).Include(i=>i.ProductAttributeItems).ThenInclude(t=>t.AttributeItem).FirstOrDefault(f=>f.Id == product.Id);
                prd.ProductAttributeItems.Clear();
                await _context.SaveChangesAsync();
                if (AttributeItemId != null) {
                    foreach (var k in AttributeItemId) {
                        prd.ProductAttributeItems.Add(new ProductAttributeItem() {ProductId=prd.Id,AttributeItemId=k,AppTenantId=tenant.AppTenantId});
                    }
                }
                await _context.SaveChangesAsync();
                prd.ShippingPrices.Clear();
                await _context.SaveChangesAsync();
                var zones = _context.ShippingZones.ToList().ToArray();
                if (ShippingPrice != null) {
                    var x = 0;
                    foreach (var s in ShippingPrice) {
                        prd.ShippingPrices.Add(new Models.ShippingPrice() {ProductId=prd.Id,CreateDate=DateTime.Now,CreatedBy=User.Identity.Name ?? "username",UpdateDate=DateTime.Now,UpdatedBy=User.Identity.Name??"username",ShippingZoneId=zones[x].Id, AppTenantId=tenant.AppTenantId, Price=s});
                        x++;
                    }
                }
                await _context.SaveChangesAsync();
                prd.ProductMedias.Clear();
                await _context.SaveChangesAsync();
                if (Media != null) {
                    var index = 0;
                    foreach (var j in Media) {
                        if (!string.IsNullOrEmpty(j)) {
                            prd.ProductMedias.Add(new ProductMedia() {ProductId=prd.Id,MediaUrl=j,Position=index});
                        }
                        index++;
                    }
                }
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewData["LanguageId"] = new SelectList(_context.Languages, "Id", "Culture", product.LanguageId);
            ViewBag.CategoryList = GetProductCategories();            
            ViewBag.CheckList = product.ProductProductCategories;
            ViewBag.Suppliers = new SelectList(_context.Suppliers,"Id","Name",supplierId);
            ViewBag.ShippingClasses = new SelectList(_context.ShippingClasses,"Id","ShippingClassName",product.ShippingClassId);
            ViewBag.Attributes = new SelectList(_context.Attributes,"Id","Name");
            ViewBag.ShippingCities = new SelectList(_context.Regions.Where(r => r.RegionType == RegionType.City).ToList(),"Id","Name");
            ViewBag.ShippingZones = new SelectList(_context.ShippingZones.ToList(),"Id","Name");
            return View(product);
        }

        public void UpdateProductProductCategories(long productId, string SelectedCategories)
        {
            string tenantId = tenant.AppTenantId;
            var ppc = _context.SetFiltered<ProductProductCategory>().Where(x => x.AppTenantId == tenantId).Where(f => f.ProductId == productId).ToList();
            var product = _context.SetFiltered<Product>().Where(x => x.AppTenantId == tenantId).Include("ProductProductCategories").Where(f => f.Id == productId).FirstOrDefault();
            if (product.ProductProductCategories == null) {
                product.ProductProductCategories = new HashSet<ProductProductCategory>();
            }
            if (SelectedCategories != null)
            {
                foreach (var c in ppc)
                {
                    _context.ProductProductCategories.Remove(c);
                }
                _context.SaveChanges();
                var cateArray = SelectedCategories.Split(',');

                foreach (var item in cateArray)
                {
                    product.ProductProductCategories.Add(new ProductProductCategory { ProductId = product.Id, ProductCategoryId = Convert.ToInt64(item), AppTenantId = tenantId });
                }
            }
            _context.Update(product);
            _context.SaveChanges();
        }
        [Authorize(Roles="ADMIN,ProductEdit")]
        // GET: CmsCore/Products/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long? supplierId = null;
            if (User.IsInRole("Supplier")) {
                supplierId = _context.Suppliers.FirstOrDefault(s=>s.UserName.ToLower() == User.Identity.Name.ToLower())?.Id;
            }
            var product = await _context.Products.Include(i=>i.ShippingPrices).Include(i=>i.ProductMedias).Include(p=>p.ProductAttributeItems).ThenInclude(t=>t.AttributeItem).Include(i=>i.ProductProductCategories).SingleOrDefaultAsync(m => m.Id == id && (supplierId.HasValue?m.SupplierId==supplierId:true));
            if (product == null)
            {
                return NotFound();
            }
            ViewData["LanguageId"] = new SelectList(_context.Languages, "Id", "Culture", product.LanguageId);
            ViewBag.CategoryList = GetProductCategories();            
            ViewBag.CheckList = product.ProductProductCategories;
            ViewBag.Suppliers = new SelectList(_context.Suppliers,"Id","Name",product.SupplierId);
            ViewBag.ShippingClasses = new SelectList(_context.ShippingClasses,"Id","ShippingClassName",product.ShippingClassId);
            ViewBag.Attributes = new SelectList(_context.Attributes,"Id","Name");
            ViewBag.AttributesList = _context.Attributes.Include(a=>a.AttributeItems).ToList();
            ViewBag.ShippingCities = new SelectList(_context.Regions.Where(r => r.RegionType == RegionType.City).ToList(),"Id","Name",product.ShippingCityId);
            ViewBag.ShippingZones = new SelectList(_context.ShippingZones.ToList(),"Id","Name",product.ShippingZoneId);
            return View(product);
        }
        [Authorize(Roles="ADMIN,ProductEdit")]
        // POST: CmsCore/Products/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("ShippingClassId,CatalogVisibility,SupplierId,AdditionalInfo,IsNew,IsApproved,Name,Slug,Description,LanguageId,UnitPrice,SalePrice,TaxStatus,TaxClass,StockCode,StockCount,StockStatus,ShippingCityId,ShippingZoneId,ShippingMethod,ProductType,ProductUrl,PurchaseNote,MenuOrder,ProductImage,ShortDescription,ViewCount,SaleCount,IsFeatured,Id,CreateDate,CreatedBy,UpdateDate,UpdatedBy,AppTenantId")] Product product, string categoriesHidden, long[] AttributeItemId, string[] Media, float[] ShippingPrice)
        {
            if (id != product.Id)
            {
                return NotFound();
            }
            long? supplierId = null;
            if (User.IsInRole("Supplier")) {
                supplierId = _context.Suppliers.FirstOrDefault(s=>s.UserName.ToLower() == User.Identity.Name.ToLower())?.Id;
            }
            if (ModelState.IsValid)
            {
                try
                {
                    product.SupplierId = supplierId;
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    UpdateProductProductCategories(product.Id, categoriesHidden);
                    var prd = _context.Products.Include(i=>i.ShippingPrices).Include(i=>i.ProductMedias).Include(i=>i.ProductAttributeItems).ThenInclude(t=>t.AttributeItem).FirstOrDefault(f=>f.Id == product.Id);
                    prd.ProductAttributeItems.Clear();
                    await _context.SaveChangesAsync();
                    if (AttributeItemId != null) {
                        foreach (var k in AttributeItemId) {
                            prd.ProductAttributeItems.Add(new ProductAttributeItem() {ProductId=prd.Id,AttributeItemId=k,AppTenantId=tenant.AppTenantId});
                        }
                    }
                    await _context.SaveChangesAsync();
                    prd.ShippingPrices.Clear();
                    await _context.SaveChangesAsync();
                    var zones = _context.ShippingZones.ToList().ToArray();
                    if (ShippingPrice != null) {
                        var x = 0;
                        foreach (var s in ShippingPrice) {
                            prd.ShippingPrices.Add(new Models.ShippingPrice() {ProductId=prd.Id,CreateDate=DateTime.Now,CreatedBy=User.Identity.Name ?? "username",UpdateDate=DateTime.Now,UpdatedBy=User.Identity.Name??"username",ShippingZoneId=zones[x].Id, AppTenantId=tenant.AppTenantId, Price=s});
                            x++;
                        }
                    }
                    await _context.SaveChangesAsync();
                    prd.ProductMedias.Clear();
                    await _context.SaveChangesAsync();
                    if (Media != null) {
                        var index = 0;
                        foreach (var j in Media) {
                            if (!string.IsNullOrEmpty(j)) {
                                prd.ProductMedias.Add(new ProductMedia() {ProductId=prd.Id,MediaUrl=j,Position=index});
                            }
                            index++;
                        }
                    }
                await _context.SaveChangesAsync();
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
           
            ViewData["LanguageId"] = new SelectList(_context.Languages, "Id", "Culture", product.LanguageId);
           
            ViewBag.CategoryList = GetProductCategories();            
            ViewBag.CheckList = product.ProductProductCategories;
            ViewBag.Suppliers = new SelectList(_context.Suppliers,"Id","Name",product.SupplierId);
            ViewBag.ShippingClasses = new SelectList(_context.ShippingClasses,"Id","ShippingClassName",product.ShippingClassId);
            ViewBag.Attributes = new SelectList(_context.Attributes,"Id","Name");
            ViewBag.AttributesList = _context.Attributes.Include(a=>a.AttributeItems).ToList();
            ViewBag.ShippingCities = new SelectList(_context.Regions.Where(r => r.RegionType == RegionType.City).ToList(),"Id","Name",product.ShippingCityId);
            ViewBag.ShippingZones = new SelectList(_context.ShippingZones.ToList(),"Id","Name",product.ShippingZoneId);
            return View(product);
        }
        [Authorize(Roles="ADMIN,ProductDelete")]
        // GET: CmsCore/Products/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long? supplierId = null;
            if (User.IsInRole("Supplier")) {
                supplierId = _context.Suppliers.FirstOrDefault(s=>s.UserName.ToLower() == User.Identity.Name.ToLower())?.Id;
            }
            var product = await _context.Products
                .Include(p => p.Language)
                .SingleOrDefaultAsync(m => m.Id == id && (supplierId.HasValue?m.SupplierId == supplierId:true));
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
        [Authorize(Roles="ADMIN,ProductDelete")]
        // POST: CmsCore/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            long? supplierId = null;
            if (User.IsInRole("Supplier")) {
                supplierId = _context.Suppliers.FirstOrDefault(s=>s.UserName.ToLower() == User.Identity.Name.ToLower())?.Id;
            }
            var product = await _context.Products.SingleOrDefaultAsync(m => m.Id == id && (supplierId.HasValue?m.SupplierId == supplierId:true));
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        private bool ProductExists(long id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        public IEnumerable<ProductCategory> GetProductCategories()
        {
            var productCategories = _context.ProductCategories.AsQueryable().Include("ChildCategories").ToList();
            
            return productCategories;

        }
    }
}
