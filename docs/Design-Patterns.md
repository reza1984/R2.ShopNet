# Gang of Four Design Patterns - R2.ShopNet Implementation

This document outlines how the 23 Gang of Four design patterns are implemented throughout the R2.ShopNet shopping platform.

---

## Table of Contents

### Creational Patterns
1. [Abstract Factory](#1-abstract-factory)
2. [Builder](#2-builder)
3. [Factory Method](#3-factory-method)
4. [Prototype](#4-prototype)
5. [Singleton](#5-singleton)

### Structural Patterns
6. [Adapter](#6-adapter)
7. [Bridge](#7-bridge)
8. [Composite](#8-composite)
9. [Decorator](#9-decorator)
10. [Facade](#10-facade)
11. [Flyweight](#11-flyweight)
12. [Proxy](#12-proxy)

### Behavioral Patterns
13. [Chain of Responsibility](#13-chain-of-responsibility)
14. [Command](#14-command)
15. [Interpreter](#15-interpreter)
16. [Iterator](#16-iterator)
17. [Mediator](#17-mediator)
18. [Memento](#18-memento)
19. [Observer](#19-observer)
20. [State](#20-state)
21. [Strategy](#21-strategy)
22. [Template Method](#22-template-method)
23. [Visitor](#23-visitor)

---

## Creational Patterns

### 1. Abstract Factory

**Purpose**: Create families of related objects without specifying their concrete classes.

**Usage in R2.ShopNet**: Payment processing system

```csharp
namespace R2.ShopNet.Payment.Domain.Factories;

// Abstract Factory
public interface IPaymentGatewayFactory
{
    IPaymentProcessor CreatePaymentProcessor();
    IRefundProcessor CreateRefundProcessor();
    IPaymentValidator CreatePaymentValidator();
}

// Concrete Factory for Stripe
public class StripePaymentFactory : IPaymentGatewayFactory
{
    public IPaymentProcessor CreatePaymentProcessor() => new StripePaymentProcessor();
    public IRefundProcessor CreateRefundProcessor() => new StripeRefundProcessor();
    public IPaymentValidator CreatePaymentValidator() => new StripePaymentValidator();
}

// Concrete Factory for PayPal
public class PayPalPaymentFactory : IPaymentGatewayFactory
{
    public IPaymentProcessor CreatePaymentProcessor() => new PayPalPaymentProcessor();
    public IRefundProcessor CreateRefundProcessor() => new PayPalRefundProcessor();
    public IPaymentValidator CreatePaymentValidator() => new PayPalPaymentValidator();
}

// Usage
public class PaymentService
{
    private readonly IPaymentGatewayFactory _factory;

    public PaymentService(string paymentMethod)
    {
        _factory = paymentMethod switch
        {
            "stripe" => new StripePaymentFactory(),
            "paypal" => new PayPalPaymentFactory(),
            "bank" => new BankTransferPaymentFactory(),
            _ => throw new NotSupportedException()
        };
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        var processor = _factory.CreatePaymentProcessor();
        var validator = _factory.CreatePaymentValidator();

        await validator.ValidateAsync(request);
        return await processor.ProcessAsync(request);
    }
}
```

**Services**: Payment, Notifications

---

### 2. Builder

**Purpose**: Construct complex objects step by step.

**Usage in R2.ShopNet**: Order creation, Product creation, Search query building

```csharp
namespace R2.ShopNet.Orders.Domain.Builders;

// Product
public interface IOrder
{
    int Id { get; }
    List<OrderItem> Items { get; }
    Address ShippingAddress { get; }
    PaymentInfo PaymentInfo { get; }
    decimal Total { get; }
}

// Builder Interface
public interface IOrderBuilder
{
    IOrderBuilder WithCustomer(int customerId);
    IOrderBuilder AddItem(int productId, int quantity, decimal price);
    IOrderBuilder WithShippingAddress(Address address);
    IOrderBuilder WithPaymentInfo(PaymentInfo paymentInfo);
    IOrderBuilder ApplyDiscount(decimal discountAmount);
    IOrderBuilder WithShippingMethod(ShippingMethod method);
    Order Build();
}

// Concrete Builder
public class OrderBuilder : IOrderBuilder
{
    private readonly Order _order;
    private readonly List<OrderItem> _items;
    private decimal _discountAmount;

    public OrderBuilder()
    {
        _order = new Order();
        _items = new List<OrderItem>();
    }

    public IOrderBuilder WithCustomer(int customerId)
    {
        _order.CustomerId = customerId;
        return this;
    }

    public IOrderBuilder AddItem(int productId, int quantity, decimal price)
    {
        _items.Add(new OrderItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = price
        });
        return this;
    }

    public IOrderBuilder WithShippingAddress(Address address)
    {
        _order.ShippingAddress = address;
        return this;
    }

    public IOrderBuilder WithPaymentInfo(PaymentInfo paymentInfo)
    {
        _order.PaymentInfo = paymentInfo;
        return this;
    }

    public IOrderBuilder ApplyDiscount(decimal discountAmount)
    {
        _discountAmount = discountAmount;
        return this;
    }

    public IOrderBuilder WithShippingMethod(ShippingMethod method)
    {
        _order.ShippingMethod = method;
        return this;
    }

    public Order Build()
    {
        _order.Items = _items;
        _order.SubTotal = _items.Sum(i => i.Quantity * i.UnitPrice);
        _order.DiscountAmount = _discountAmount;
        _order.ShippingCost = CalculateShippingCost();
        _order.Total = _order.SubTotal - _discountAmount + _order.ShippingCost;
        _order.OrderDate = DateTime.UtcNow;
        _order.Status = OrderStatus.Pending;

        return _order;
    }

    private decimal CalculateShippingCost()
    {
        return _order.ShippingMethod switch
        {
            ShippingMethod.Standard => 5.00m,
            ShippingMethod.Express => 15.00m,
            ShippingMethod.SameDay => 25.00m,
            _ => 0m
        };
    }
}

// Usage
var order = new OrderBuilder()
    .WithCustomer(customerId)
    .AddItem(productId1, 2, 29.99m)
    .AddItem(productId2, 1, 49.99m)
    .WithShippingAddress(address)
    .WithPaymentInfo(paymentInfo)
    .ApplyDiscount(10.00m)
    .WithShippingMethod(ShippingMethod.Express)
    .Build();
```

**Services**: Orders, Catalog (Product), Search (Query Builder), Notifications (Message Builder)

---

### 3. Factory Method

**Purpose**: Define an interface for creating objects, but let subclasses decide which class to instantiate.

**Usage in R2.ShopNet**: Repository creation, Service creation

```csharp
namespace R2.ShopNet.Common.Factories;

// Creator
public abstract class RepositoryFactory
{
    public abstract IRepository<T> CreateRepository<T>() where T : class;
}

// Concrete Creator for SQL
public class SqlRepositoryFactory : RepositoryFactory
{
    private readonly ApplicationDbContext _context;

    public SqlRepositoryFactory(ApplicationDbContext context)
    {
        _context = context;
    }

    public override IRepository<T> CreateRepository<T>()
    {
        return new SqlRepository<T>(_context);
    }
}

// Concrete Creator for NoSQL
public class NoSqlRepositoryFactory : RepositoryFactory
{
    private readonly IMongoDatabase _database;

    public NoSqlRepositoryFactory(IMongoDatabase database)
    {
        _database = database;
    }

    public override IRepository<T> CreateRepository<T>()
    {
        return new MongoRepository<T>(_database);
    }
}

// Usage in Domain Service
public class ProductService
{
    private readonly IRepository<Product> _productRepository;

    public ProductService(RepositoryFactory factory)
    {
        _productRepository = factory.CreateRepository<Product>();
    }
}
```

**Services**: All services (Repository creation), Notifications (Channel factory), Delivery (Route calculator factory)

---

### 4. Prototype

**Purpose**: Create new objects by copying existing objects.

**Usage in R2.ShopNet**: Product duplication, Order templates, Cart cloning

```csharp
namespace R2.ShopNet.Catalog.Domain.Entities;

public interface IPrototype<T>
{
    T Clone();
    T DeepClone();
}

public class Product : AggregateRoot, IPrototype<Product>
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public List<ProductImage> Images { get; set; }
    public List<ProductVariant> Variants { get; set; }

    // Shallow copy
    public Product Clone()
    {
        return (Product)this.MemberwiseClone();
    }

    // Deep copy
    public Product DeepClone()
    {
        var cloned = (Product)this.MemberwiseClone();
        cloned.Images = this.Images.Select(img => new ProductImage
        {
            Url = img.Url,
            Alt = img.Alt,
            IsPrimary = img.IsPrimary
        }).ToList();

        cloned.Variants = this.Variants.Select(v => new ProductVariant
        {
            SKU = v.SKU + "-COPY",
            Size = v.Size,
            Color = v.Color,
            Price = v.Price,
            Stock = 0 // Reset stock for new product
        }).ToList();

        return cloned;
    }
}

// Usage: Duplicate product with variants
var originalProduct = await _productRepository.GetByIdAsync(productId);
var newProduct = originalProduct.DeepClone();
newProduct.Name = originalProduct.Name + " (Copy)";
newProduct.Id = 0; // New ID will be generated
await _productRepository.AddAsync(newProduct);
```

**Services**: Catalog (Product duplication), Orders (Order templates), Cart (Save for later)

---

### 5. Singleton

**Purpose**: Ensure a class has only one instance and provide global access to it.

**Usage in R2.ShopNet**: Configuration, Logging, Cache managers

```csharp
namespace R2.ShopNet.Common.Infrastructure;

// Thread-safe Singleton using Lazy<T>
public sealed class CacheManager
{
    private static readonly Lazy<CacheManager> _instance =
        new Lazy<CacheManager>(() => new CacheManager());

    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheManager> _logger;

    private CacheManager()
    {
        // Initialize cache connection
        var connection = ConnectionMultiplexer.Connect("localhost:6379");
        _cache = connection.GetDatabase();
    }

    public static CacheManager Instance => _instance.Value;

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        var cached = await _cache.GetStringAsync(key);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<T>(cached);
        }

        var value = await factory();
        var serialized = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });

        return value;
    }
}

// Better approach with DI - Singleton lifetime
services.AddSingleton<ICacheManager, CacheManager>();
```

**Services**: Common (Cache Manager, Configuration Manager), All services (Logger instances)

---

## Structural Patterns

### 6. Adapter

**Purpose**: Convert the interface of a class into another interface clients expect.

**Usage in R2.ShopNet**: External API integration, Legacy system integration

```csharp
namespace R2.ShopNet.Delivery.Infrastructure.Adapters;

// Target interface
public interface IRoutingService
{
    Task<RouteInfo> CalculateRouteAsync(Location from, Location to);
    Task<TimeSpan> EstimateDeliveryTimeAsync(Location from, Location to);
}

// Adaptee (Google Maps API)
public class GoogleMapsClient
{
    public async Task<GoogleDirectionsResponse> GetDirectionsAsync(
        double fromLat, double fromLng,
        double toLat, double toLng)
    {
        // Google Maps API call
        return await _httpClient.GetFromJsonAsync<GoogleDirectionsResponse>($"...");
    }
}

// Adapter
public class GoogleMapsAdapter : IRoutingService
{
    private readonly GoogleMapsClient _googleMapsClient;

    public GoogleMapsAdapter(GoogleMapsClient googleMapsClient)
    {
        _googleMapsClient = googleMapsClient;
    }

    public async Task<RouteInfo> CalculateRouteAsync(Location from, Location to)
    {
        var response = await _googleMapsClient.GetDirectionsAsync(
            from.Latitude, from.Longitude,
            to.Latitude, to.Longitude);

        // Adapt Google response to our domain model
        return new RouteInfo
        {
            Distance = response.Routes[0].Legs[0].Distance.Value,
            Duration = TimeSpan.FromSeconds(response.Routes[0].Legs[0].Duration.Value),
            Steps = response.Routes[0].Legs[0].Steps.Select(s => new RouteStep
            {
                Instruction = s.HtmlInstructions,
                Distance = s.Distance.Value
            }).ToList()
        };
    }

    public async Task<TimeSpan> EstimateDeliveryTimeAsync(Location from, Location to)
    {
        var route = await CalculateRouteAsync(from, to);
        return route.Duration;
    }
}

// Alternative adapter for OpenStreetMap
public class OpenStreetMapAdapter : IRoutingService
{
    // Different implementation, same interface
}

// Usage with DI
services.AddScoped<IRoutingService, GoogleMapsAdapter>();
// Or switch to: services.AddScoped<IRoutingService, OpenStreetMapAdapter>();
```

**Services**: Delivery (Routing APIs), Payment (Payment gateways), Notifications (SMS providers)

---

### 7. Bridge

**Purpose**: Decouple abstraction from implementation so they can vary independently.

**Usage in R2.ShopNet**: Notification system with multiple channels

```csharp
namespace R2.ShopNet.Notifications.Domain;

// Implementor
public interface INotificationSender
{
    Task SendAsync(string recipient, string subject, string body);
}

// Concrete Implementors
public class EmailSender : INotificationSender
{
    public async Task SendAsync(string recipient, string subject, string body)
    {
        // SMTP email sending logic
        await _smtpClient.SendMailAsync(new MailMessage
        {
            To = { recipient },
            Subject = subject,
            Body = body
        });
    }
}

public class SmsSender : INotificationSender
{
    public async Task SendAsync(string recipient, string subject, string body)
    {
        // SMS API logic
        await _smsClient.SendAsync(recipient, body);
    }
}

public class PushNotificationSender : INotificationSender
{
    public async Task SendAsync(string recipient, string subject, string body)
    {
        // Firebase Cloud Messaging logic
        await _fcmClient.SendAsync(recipient, new Notification
        {
            Title = subject,
            Body = body
        });
    }
}

// Abstraction
public abstract class Notification
{
    protected INotificationSender _sender;

    protected Notification(INotificationSender sender)
    {
        _sender = sender;
    }

    public abstract Task SendAsync(string recipient);
}

// Refined Abstractions
public class OrderConfirmationNotification : Notification
{
    private readonly Order _order;

    public OrderConfirmationNotification(Order order, INotificationSender sender)
        : base(sender)
    {
        _order = order;
    }

    public override async Task SendAsync(string recipient)
    {
        var subject = $"Order Confirmation #{_order.Id}";
        var body = $"Thank you for your order. Total: ${_order.Total}";
        await _sender.SendAsync(recipient, subject, body);
    }
}

public class DeliveryNotification : Notification
{
    private readonly Delivery _delivery;

    public DeliveryNotification(Delivery delivery, INotificationSender sender)
        : base(sender)
    {
        _delivery = delivery;
    }

    public override async Task SendAsync(string recipient)
    {
        var subject = "Your order is out for delivery";
        var body = $"Driver: {_delivery.DriverName}. Track: {_delivery.TrackingUrl}";
        await _sender.SendAsync(recipient, subject, body);
    }
}

// Usage
var emailSender = new EmailSender();
var smsSender = new SmsSender();

var orderNotification = new OrderConfirmationNotification(order, emailSender);
await orderNotification.SendAsync("customer@email.com");

var deliveryNotification = new DeliveryNotification(delivery, smsSender);
await deliveryNotification.SendAsync("+1234567890");
```

**Services**: Notifications (Multiple channels), Catalog (Multiple search engines), Payment (Multiple gateways)

---

### 8. Composite

**Purpose**: Compose objects into tree structures to represent part-whole hierarchies.

**Usage in R2.ShopNet**: Category hierarchy, Order items with bundles, Discount rules

```csharp
namespace R2.ShopNet.Catalog.Domain.Entities;

// Component
public abstract class CatalogComponent
{
    public int Id { get; set; }
    public string Name { get; set; }
    public abstract decimal GetPrice();
    public abstract void Display(int depth = 0);
}

// Leaf
public class Product : CatalogComponent
{
    public decimal Price { get; set; }
    public string SKU { get; set; }

    public override decimal GetPrice() => Price;

    public override void Display(int depth = 0)
    {
        Console.WriteLine(new string('-', depth) + $" Product: {Name} (${Price})");
    }
}

// Composite
public class ProductBundle : CatalogComponent
{
    private readonly List<CatalogComponent> _items = new();
    public decimal DiscountPercentage { get; set; }

    public void Add(CatalogComponent component)
    {
        _items.Add(component);
    }

    public void Remove(CatalogComponent component)
    {
        _items.Remove(component);
    }

    public override decimal GetPrice()
    {
        var total = _items.Sum(item => item.GetPrice());
        return total * (1 - DiscountPercentage / 100);
    }

    public override void Display(int depth = 0)
    {
        Console.WriteLine(new string('-', depth) + $" Bundle: {Name} (${GetPrice()})");
        foreach (var item in _items)
        {
            item.Display(depth + 2);
        }
    }
}

// Usage: Create product bundles
var laptop = new Product { Name = "Laptop", Price = 999.99m };
var mouse = new Product { Name = "Mouse", Price = 29.99m };
var keyboard = new Product { Name = "Keyboard", Price = 79.99m };

var officeBundle = new ProductBundle
{
    Name = "Office Bundle",
    DiscountPercentage = 10 // 10% discount on bundle
};
officeBundle.Add(laptop);
officeBundle.Add(mouse);
officeBundle.Add(keyboard);

Console.WriteLine($"Bundle Price: ${officeBundle.GetPrice()}");
officeBundle.Display();
```

**Another example: Category hierarchy**

```csharp
// Component
public abstract class CategoryComponent
{
    public int Id { get; set; }
    public string Name { get; set; }
    public abstract int GetProductCount();
    public abstract List<Product> GetAllProducts();
}

// Leaf (Category with products)
public class Category : CategoryComponent
{
    public List<Product> Products { get; set; } = new();

    public override int GetProductCount() => Products.Count;

    public override List<Product> GetAllProducts() => Products;
}

// Composite (Parent category with subcategories)
public class CategoryGroup : CategoryComponent
{
    private readonly List<CategoryComponent> _subcategories = new();

    public void AddSubcategory(CategoryComponent category)
    {
        _subcategories.Add(category);
    }

    public override int GetProductCount()
    {
        return _subcategories.Sum(cat => cat.GetProductCount());
    }

    public override List<Product> GetAllProducts()
    {
        return _subcategories.SelectMany(cat => cat.GetAllProducts()).ToList();
    }
}

// Usage: Electronics -> Laptops, Phones
var electronics = new CategoryGroup { Name = "Electronics" };
var laptops = new Category { Name = "Laptops" };
var phones = new Category { Name = "Phones" };

electronics.AddSubcategory(laptops);
electronics.AddSubcategory(phones);

Console.WriteLine($"Total products in Electronics: {electronics.GetProductCount()}");
```

**Services**: Catalog (Categories, Product bundles), Orders (Order items), Authorization (Permission hierarchy)

---

### 9. Decorator

**Purpose**: Attach additional responsibilities to an object dynamically.

**Usage in R2.ShopNet**: Pricing with discounts, Notification with logging, Repository with caching

```csharp
namespace R2.ShopNet.Orders.Domain.Decorators;

// Component
public interface IPricingService
{
    decimal CalculatePrice(Order order);
}

// Concrete Component
public class BasePricingService : IPricingService
{
    public decimal CalculatePrice(Order order)
    {
        return order.Items.Sum(item => item.Quantity * item.UnitPrice);
    }
}

// Decorator
public abstract class PricingDecorator : IPricingService
{
    protected readonly IPricingService _pricingService;

    protected PricingDecorator(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    public virtual decimal CalculatePrice(Order order)
    {
        return _pricingService.CalculatePrice(order);
    }
}

// Concrete Decorators
public class DiscountDecorator : PricingDecorator
{
    private readonly decimal _discountPercentage;

    public DiscountDecorator(IPricingService pricingService, decimal discountPercentage)
        : base(pricingService)
    {
        _discountPercentage = discountPercentage;
    }

    public override decimal CalculatePrice(Order order)
    {
        var basePrice = base.CalculatePrice(order);
        return basePrice * (1 - _discountPercentage / 100);
    }
}

public class TaxDecorator : PricingDecorator
{
    private readonly decimal _taxRate;

    public TaxDecorator(IPricingService pricingService, decimal taxRate)
        : base(pricingService)
    {
        _taxRate = taxRate;
    }

    public override decimal CalculatePrice(Order order)
    {
        var basePrice = base.CalculatePrice(order);
        return basePrice * (1 + _taxRate / 100);
    }
}

public class ShippingDecorator : PricingDecorator
{
    private readonly decimal _shippingCost;

    public ShippingDecorator(IPricingService pricingService, decimal shippingCost)
        : base(pricingService)
    {
        _shippingCost = shippingCost;
    }

    public override decimal CalculatePrice(Order order)
    {
        return base.CalculatePrice(order) + _shippingCost;
    }
}

// Usage: Stack decorators
IPricingService pricingService = new BasePricingService();
pricingService = new DiscountDecorator(pricingService, 10); // 10% discount
pricingService = new TaxDecorator(pricingService, 8); // 8% tax
pricingService = new ShippingDecorator(pricingService, 5.99m); // $5.99 shipping

var totalPrice = pricingService.CalculatePrice(order);
```

**Another example: Repository with caching**

```csharp
// Decorator for repository with caching
public class CachedRepositoryDecorator<T> : IRepository<T> where T : class
{
    private readonly IRepository<T> _repository;
    private readonly ICacheManager _cacheManager;

    public CachedRepositoryDecorator(IRepository<T> repository, ICacheManager cacheManager)
    {
        _repository = repository;
        _cacheManager = cacheManager;
    }

    public async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var cacheKey = $"{typeof(T).Name}_{id}";

        return await _cacheManager.GetOrSetAsync(
            cacheKey,
            () => _repository.GetByIdAsync(id, cancellationToken),
            TimeSpan.FromMinutes(10)
        );
    }

    // Other methods...
}

// Registration
services.Decorate<IRepository<Product>, CachedRepositoryDecorator<Product>>();
```

**Services**: Orders (Pricing), All services (Repository caching), Notifications (Logging decorator)

---

### 10. Facade

**Purpose**: Provide a unified interface to a set of interfaces in a subsystem.

**Usage in R2.ShopNet**: Checkout process, Order fulfillment

```csharp
namespace R2.ShopNet.Orders.Application.Facades;

// Complex subsystem classes
public class InventoryService
{
    public async Task<bool> ReserveStockAsync(int productId, int quantity) { /* ... */ }
}

public class PaymentService
{
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentInfo info) { /* ... */ }
}

public class OrderService
{
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request) { /* ... */ }
}

public class NotificationService
{
    public async Task SendOrderConfirmationAsync(Order order) { /* ... */ }
}

public class ShippingService
{
    public async Task CreateShipmentAsync(Order order) { /* ... */ }
}

// Facade - Simplified interface
public class CheckoutFacade
{
    private readonly InventoryService _inventoryService;
    private readonly PaymentService _paymentService;
    private readonly OrderService _orderService;
    private readonly NotificationService _notificationService;
    private readonly ShippingService _shippingService;
    private readonly ILogger<CheckoutFacade> _logger;

    public CheckoutFacade(
        InventoryService inventoryService,
        PaymentService paymentService,
        OrderService orderService,
        NotificationService notificationService,
        ShippingService shippingService,
        ILogger<CheckoutFacade> logger)
    {
        _inventoryService = inventoryService;
        _paymentService = paymentService;
        _orderService = orderService;
        _notificationService = notificationService;
        _shippingService = shippingService;
        _logger = logger;
    }

    public async Task<CheckoutResult> ProcessCheckoutAsync(CheckoutRequest request)
    {
        try
        {
            // Step 1: Reserve inventory
            _logger.LogInformation("Reserving inventory...");
            foreach (var item in request.Items)
            {
                var reserved = await _inventoryService.ReserveStockAsync(
                    item.ProductId, item.Quantity);

                if (!reserved)
                {
                    return CheckoutResult.Failure("Insufficient stock");
                }
            }

            // Step 2: Process payment
            _logger.LogInformation("Processing payment...");
            var paymentResult = await _paymentService.ProcessPaymentAsync(request.PaymentInfo);
            if (!paymentResult.IsSuccess)
            {
                // Release reserved stock
                await ReleaseReservedStockAsync(request.Items);
                return CheckoutResult.Failure("Payment failed");
            }

            // Step 3: Create order
            _logger.LogInformation("Creating order...");
            var order = await _orderService.CreateOrderAsync(new CreateOrderRequest
            {
                CustomerId = request.CustomerId,
                Items = request.Items,
                ShippingAddress = request.ShippingAddress,
                PaymentId = paymentResult.TransactionId
            });

            // Step 4: Create shipment
            _logger.LogInformation("Creating shipment...");
            await _shippingService.CreateShipmentAsync(order);

            // Step 5: Send confirmation
            _logger.LogInformation("Sending confirmation...");
            await _notificationService.SendOrderConfirmationAsync(order);

            return CheckoutResult.Success(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checkout process failed");
            // Compensating actions
            await ReleaseReservedStockAsync(request.Items);
            return CheckoutResult.Failure($"Checkout failed: {ex.Message}");
        }
    }

    private async Task ReleaseReservedStockAsync(List<OrderItem> items)
    {
        foreach (var item in items)
        {
            await _inventoryService.ReleaseStockAsync(item.ProductId, item.Quantity);
        }
    }
}

// Usage - Simple API for clients
public class CheckoutController : ControllerBase
{
    private readonly CheckoutFacade _checkoutFacade;

    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var result = await _checkoutFacade.ProcessCheckoutAsync(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
```

**Services**: Orders (Checkout), Warehouse (Order fulfillment), Delivery (Delivery assignment)

---

### 11. Flyweight

**Purpose**: Use sharing to support large numbers of fine-grained objects efficiently.

**Usage in R2.ShopNet**: Product attributes, Delivery locations, Tax rates

```csharp
namespace R2.ShopNet.Catalog.Domain.Flyweights;

// Flyweight - Shared immutable state
public class ProductAttribute
{
    public string Name { get; }
    public string Type { get; } // Color, Size, Material, etc.

    public ProductAttribute(string name, string type)
    {
        Name = name;
        Type = type;
    }

    // Heavy data that should be shared
    public string Description { get; set; }
    public byte[] Icon { get; set; }
}

// Flyweight Factory
public class ProductAttributeFactory
{
    private readonly Dictionary<string, ProductAttribute> _attributes = new();
    private readonly object _lock = new();

    public ProductAttribute GetAttribute(string name, string type)
    {
        var key = $"{type}:{name}";

        if (_attributes.ContainsKey(key))
        {
            return _attributes[key];
        }

        lock (_lock)
        {
            if (!_attributes.ContainsKey(key))
            {
                _attributes[key] = new ProductAttribute(name, type);
            }
        }

        return _attributes[key];
    }

    public int GetTotalAttributes() => _attributes.Count;
}

// Context - Stores extrinsic state
public class ProductVariant
{
    public int Id { get; set; }
    public string SKU { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }

    // Intrinsic state - shared via Flyweight
    public ProductAttribute Color { get; set; }
    public ProductAttribute Size { get; set; }
    public ProductAttribute Material { get; set; }
}

// Usage
var attributeFactory = new ProductAttributeFactory();

// Create 1000 products with variants
// Instead of creating 3000 attribute objects (1000 products * 3 attributes),
// We reuse shared attribute objects
for (int i = 0; i < 1000; i++)
{
    var variant = new ProductVariant
    {
        SKU = $"PROD-{i}",
        Price = 29.99m,
        Stock = 100,
        // Reuse shared flyweight objects
        Color = attributeFactory.GetAttribute("Red", "Color"),
        Size = attributeFactory.GetAttribute("Large", "Size"),
        Material = attributeFactory.GetAttribute("Cotton", "Material")
    };
}

Console.WriteLine($"Total unique attributes: {attributeFactory.GetTotalAttributes()}");
// Output: Total unique attributes: 3 (instead of 3000)
```

**Another example: Delivery locations cache**

```csharp
public class LocationFlyweight
{
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }

    // Heavy shared data
    public GeoCoordinates Coordinates { get; set; }
    public List<string> DeliveryZones { get; set; }

    public LocationFlyweight(string city, string state, string zipCode)
    {
        City = city;
        State = state;
        ZipCode = zipCode;
    }
}

public class LocationFactory
{
    private readonly Dictionary<string, LocationFlyweight> _locations = new();

    public LocationFlyweight GetLocation(string zipCode)
    {
        if (!_locations.ContainsKey(zipCode))
        {
            // Load from database only once
            _locations[zipCode] = LoadFromDatabase(zipCode);
        }

        return _locations[zipCode];
    }
}
```

**Services**: Catalog (Product attributes), Delivery (Location data), Common (Configuration values)

---

### 12. Proxy

**Purpose**: Provide a surrogate or placeholder for another object to control access to it.

**Usage in R2.ShopNet**: Lazy loading, Access control, Logging, Caching

```csharp
namespace R2.ShopNet.Catalog.Infrastructure.Proxies;

// Subject Interface
public interface IProductRepository
{
    Task<Product> GetByIdAsync(int id);
    Task<List<Product>> GetAllAsync();
}

// Real Subject
public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<Product> GetByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
    }
}

// Protection Proxy - Add access control
public class SecureProductRepositoryProxy : IProductRepository
{
    private readonly IProductRepository _repository;
    private readonly IAuthorizationService _authService;
    private readonly ClaimsPrincipal _user;

    public SecureProductRepositoryProxy(
        IProductRepository repository,
        IAuthorizationService authService,
        ClaimsPrincipal user)
    {
        _repository = repository;
        _authService = authService;
        _user = user;
    }

    public async Task<Product> GetByIdAsync(int id)
    {
        var authResult = await _authService.AuthorizeAsync(_user, "ViewProducts");
        if (!authResult.Succeeded)
        {
            throw new UnauthorizedAccessException("User not authorized to view products");
        }

        return await _repository.GetByIdAsync(id);
    }

    public async Task<List<Product>> GetAllAsync()
    {
        var authResult = await _authService.AuthorizeAsync(_user, "ViewProducts");
        if (!authResult.Succeeded)
        {
            throw new UnauthorizedAccessException();
        }

        return await _repository.GetAllAsync();
    }
}

// Virtual Proxy - Lazy loading with caching
public class CachedProductRepositoryProxy : IProductRepository
{
    private readonly IProductRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedProductRepositoryProxy> _logger;

    public async Task<Product> GetByIdAsync(int id)
    {
        var cacheKey = $"product_{id}";

        if (_cache.TryGetValue(cacheKey, out Product cachedProduct))
        {
            _logger.LogInformation($"Product {id} retrieved from cache");
            return cachedProduct;
        }

        _logger.LogInformation($"Product {id} not in cache, loading from database");
        var product = await _repository.GetByIdAsync(id);

        _cache.Set(cacheKey, product, TimeSpan.FromMinutes(10));

        return product;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }
}

// Logging Proxy
public class LoggingProductRepositoryProxy : IProductRepository
{
    private readonly IProductRepository _repository;
    private readonly ILogger<LoggingProductRepositoryProxy> _logger;

    public async Task<Product> GetByIdAsync(int id)
    {
        _logger.LogInformation($"GetByIdAsync called with id: {id}");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _repository.GetByIdAsync(id);
            _logger.LogInformation($"GetByIdAsync completed in {stopwatch.ElapsedMilliseconds}ms");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"GetByIdAsync failed for id: {id}");
            throw;
        }
    }

    public async Task<List<Product>> GetAllAsync()
    {
        _logger.LogInformation("GetAllAsync called");
        return await _repository.GetAllAsync();
    }
}

// Usage - Combine multiple proxies
IProductRepository repository = new ProductRepository(context);
repository = new CachedProductRepositoryProxy(repository, cache, logger);
repository = new SecureProductRepositoryProxy(repository, authService, user);
repository = new LoggingProductRepositoryProxy(repository, logger);

var product = await repository.GetByIdAsync(123);
```

**Services**: All services (Repository proxies), Catalog (Lazy loading images), Authorization (Access control)

---

## Behavioral Patterns

### 13. Chain of Responsibility

**Purpose**: Avoid coupling the sender of a request to its receiver by giving more than one object a chance to handle the request.

**Usage in R2.ShopNet**: Order validation, Discount calculation, Request pipeline

```csharp
namespace R2.ShopNet.Orders.Application.Handlers;

// Handler interface
public interface IOrderValidator
{
    IOrderValidator SetNext(IOrderValidator handler);
    Task<ValidationResult> ValidateAsync(Order order);
}

// Abstract Handler
public abstract class OrderValidatorBase : IOrderValidator
{
    private IOrderValidator _nextHandler;

    public IOrderValidator SetNext(IOrderValidator handler)
    {
        _nextHandler = handler;
        return handler;
    }

    public virtual async Task<ValidationResult> ValidateAsync(Order order)
    {
        if (_nextHandler != null)
        {
            return await _nextHandler.ValidateAsync(order);
        }

        return ValidationResult.Success();
    }
}

// Concrete Handlers
public class StockAvailabilityValidator : OrderValidatorBase
{
    private readonly IInventoryService _inventoryService;

    public override async Task<ValidationResult> ValidateAsync(Order order)
    {
        foreach (var item in order.Items)
        {
            var stock = await _inventoryService.GetStockAsync(item.ProductId);
            if (stock < item.Quantity)
            {
                return ValidationResult.Failure(
                    $"Insufficient stock for product {item.ProductId}");
            }
        }

        return await base.ValidateAsync(order);
    }
}

public class MinimumOrderAmountValidator : OrderValidatorBase
{
    private const decimal MinimumAmount = 10.00m;

    public override async Task<ValidationResult> ValidateAsync(Order order)
    {
        if (order.Total < MinimumAmount)
        {
            return ValidationResult.Failure(
                $"Order total must be at least ${MinimumAmount}");
        }

        return await base.ValidateAsync(order);
    }
}

public class ShippingAddressValidator : OrderValidatorBase
{
    public override async Task<ValidationResult> ValidateAsync(Order order)
    {
        if (string.IsNullOrEmpty(order.ShippingAddress?.ZipCode))
        {
            return ValidationResult.Failure("Shipping address is required");
        }

        // Validate address with external service
        if (!await IsValidAddressAsync(order.ShippingAddress))
        {
            return ValidationResult.Failure("Invalid shipping address");
        }

        return await base.ValidateAsync(order);
    }
}

public class PaymentMethodValidator : OrderValidatorBase
{
    public override async Task<ValidationResult> ValidateAsync(Order order)
    {
        if (order.PaymentInfo == null)
        {
            return ValidationResult.Failure("Payment method is required");
        }

        // Validate payment method
        if (!IsValidPaymentMethod(order.PaymentInfo))
        {
            return ValidationResult.Failure("Invalid payment method");
        }

        return await base.ValidateAsync(order);
    }
}

// Usage - Build chain
var validator = new StockAvailabilityValidator(inventoryService);
validator
    .SetNext(new MinimumOrderAmountValidator())
    .SetNext(new ShippingAddressValidator())
    .SetNext(new PaymentMethodValidator());

var result = await validator.ValidateAsync(order);
if (!result.IsValid)
{
    throw new ValidationException(result.ErrorMessage);
}
```

**Services**: Orders (Validation chain), Payment (Fraud detection chain), Authorization (Permission checking)

---

### 14. Command

**Purpose**: Encapsulate a request as an object, thereby letting you parameterize clients with different requests, queue or log requests, and support undoable operations.

**Usage in R2.ShopNet**: CQRS Commands, Undo/Redo operations

```csharp
namespace R2.ShopNet.CQRS;

// Command Interface
public interface ICommand<TResponse>
{
    // Marker interface
}

// Command Handler Interface
public interface ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

// Concrete Commands
public class CreateProductCommand : ICommand<Result<ProductDto>>
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
}

public class UpdateProductPriceCommand : ICommand<Result<Unit>>
{
    public int ProductId { get; set; }
    public decimal NewPrice { get; set; }
    public decimal OldPrice { get; set; } // For undo
}

// Concrete Command Handlers
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;

    public async Task<Result<ProductDto>> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = command.Name,
            Description = command.Description,
            Price = command.Price,
            CategoryId = command.CategoryId
        };

        await _repository.AddAsync(product, cancellationToken);

        // Manual mapping to DTO
        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId
        };

        return Result<ProductDto>.Success(dto);
    }
}

// Command Dispatcher (Invoker)
public interface ICommandDispatcher
{
    Task<TResponse> DispatchAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default);
}

public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> DispatchAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<,>)
            .MakeGenericType(command.GetType(), typeof(TResponse));

        dynamic handler = _serviceProvider.GetRequiredService(handlerType);

        return await handler.HandleAsync((dynamic)command, cancellationToken);
    }
}

// Usage
public class ProductsController : ControllerBase
{
    private readonly ICommandDispatcher _commandDispatcher;

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _commandDispatcher.DispatchAsync(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetProduct), new { id = result.Value.Id }, result.Value)
            : BadRequest(result.Error);
    }
}
```

**Undo/Redo example:**

```csharp
// Command with undo support
public interface IUndoableCommand : ICommand<Result<Unit>>
{
    Task UndoAsync();
}

public class UpdateProductPriceUndoableCommand : IUndoableCommand
{
    private readonly IProductRepository _repository;
    public int ProductId { get; set; }
    public decimal NewPrice { get; set; }
    public decimal OldPrice { get; set; }

    public async Task<Result<Unit>> ExecuteAsync()
    {
        var product = await _repository.GetByIdAsync(ProductId);
        OldPrice = product.Price;
        product.Price = NewPrice;
        await _repository.UpdateAsync(product);
        return Result<Unit>.Success(Unit.Value);
    }

    public async Task UndoAsync()
    {
        var product = await _repository.GetByIdAsync(ProductId);
        product.Price = OldPrice;
        await _repository.UpdateAsync(product);
    }
}

// Command History
public class CommandHistory
{
    private readonly Stack<IUndoableCommand> _history = new();

    public async Task ExecuteAsync(IUndoableCommand command)
    {
        await command.ExecuteAsync();
        _history.Push(command);
    }

    public async Task UndoAsync()
    {
        if (_history.Count > 0)
        {
            var command = _history.Pop();
            await command.UndoAsync();
        }
    }
}
```

**Services**: All services (CQRS Commands), Catalog (Product operations), Orders (Order operations)

---

### 15. Interpreter

**Purpose**: Define a representation for a grammar along with an interpreter that uses the representation to interpret sentences in the language.

**Usage in R2.ShopNet**: Search query parsing, Discount rule engine, Price calculation rules

```csharp
namespace R2.ShopNet.Search.Domain.Interpreters;

// Context
public class SearchContext
{
    public string SearchText { get; set; }
    public Dictionary<string, object> Variables { get; set; } = new();
}

// Abstract Expression
public interface ISearchExpression
{
    bool Interpret(Product product, SearchContext context);
}

// Terminal Expression - Simple match
public class KeywordExpression : ISearchExpression
{
    private readonly string _keyword;

    public KeywordExpression(string keyword)
    {
        _keyword = keyword.ToLower();
    }

    public bool Interpret(Product product, SearchContext context)
    {
        return product.Name.ToLower().Contains(_keyword) ||
               product.Description.ToLower().Contains(_keyword);
    }
}

// Terminal Expression - Price range
public class PriceRangeExpression : ISearchExpression
{
    private readonly decimal _min;
    private readonly decimal _max;

    public PriceRangeExpression(decimal min, decimal max)
    {
        _min = min;
        _max = max;
    }

    public bool Interpret(Product product, SearchContext context)
    {
        return product.Price >= _min && product.Price <= _max;
    }
}

// Terminal Expression - Category
public class CategoryExpression : ISearchExpression
{
    private readonly int _categoryId;

    public CategoryExpression(int categoryId)
    {
        _categoryId = categoryId;
    }

    public bool Interpret(Product product, SearchContext context)
    {
        return product.CategoryId == _categoryId;
    }
}

// Non-terminal Expression - AND
public class AndExpression : ISearchExpression
{
    private readonly ISearchExpression _left;
    private readonly ISearchExpression _right;

    public AndExpression(ISearchExpression left, ISearchExpression right)
    {
        _left = left;
        _right = right;
    }

    public bool Interpret(Product product, SearchContext context)
    {
        return _left.Interpret(product, context) &&
               _right.Interpret(product, context);
    }
}

// Non-terminal Expression - OR
public class OrExpression : ISearchExpression
{
    private readonly ISearchExpression _left;
    private readonly ISearchExpression _right;

    public OrExpression(ISearchExpression left, ISearchExpression right)
    {
        _left = left;
        _right = right;
    }

    public bool Interpret(Product product, SearchContext context)
    {
        return _left.Interpret(product, context) ||
               _right.Interpret(product, context);
    }
}

// Non-terminal Expression - NOT
public class NotExpression : ISearchExpression
{
    private readonly ISearchExpression _expression;

    public NotExpression(ISearchExpression expression)
    {
        _expression = expression;
    }

    public bool Interpret(Product product, SearchContext context)
    {
        return !_expression.Interpret(product, context);
    }
}

// Search Query Parser
public class SearchQueryParser
{
    public ISearchExpression Parse(string query)
    {
        // Parse: "laptop AND (price:100-500 OR category:electronics)"
        // This is a simplified example

        if (query.Contains(" AND "))
        {
            var parts = query.Split(" AND ");
            var left = Parse(parts[0]);
            var right = Parse(parts[1]);
            return new AndExpression(left, right);
        }

        if (query.Contains(" OR "))
        {
            var parts = query.Split(" OR ");
            var left = Parse(parts[0]);
            var right = Parse(parts[1]);
            return new OrExpression(left, right);
        }

        if (query.StartsWith("price:"))
        {
            var range = query.Substring(6).Split('-');
            return new PriceRangeExpression(
                decimal.Parse(range[0]),
                decimal.Parse(range[1]));
        }

        if (query.StartsWith("category:"))
        {
            var categoryId = int.Parse(query.Substring(9));
            return new CategoryExpression(categoryId);
        }

        return new KeywordExpression(query);
    }
}

// Usage
var parser = new SearchQueryParser();
var expression = parser.Parse("laptop AND price:500-1000");

var products = await _productRepository.GetAllAsync();
var context = new SearchContext { SearchText = "laptop" };

var results = products.Where(p => expression.Interpret(p, context)).ToList();
```

**Another example: Discount rules**

```csharp
// Discount Rule Interpreter
public interface IDiscountRule
{
    decimal Calculate(Order order);
}

public class PercentageDiscountRule : IDiscountRule
{
    private readonly decimal _percentage;

    public decimal Calculate(Order order) => order.SubTotal * _percentage / 100;
}

public class FixedAmountDiscountRule : IDiscountRule
{
    private readonly decimal _amount;

    public decimal Calculate(Order order) => _amount;
}

public class BuyXGetYDiscountRule : IDiscountRule
{
    private readonly int _buyQuantity;
    private readonly int _getQuantity;

    public decimal Calculate(Order order)
    {
        var totalQuantity = order.Items.Sum(i => i.Quantity);
        var freeItems = (totalQuantity / _buyQuantity) * _getQuantity;
        var avgPrice = order.SubTotal / totalQuantity;
        return freeItems * avgPrice;
    }
}

// Composite rule
public class CompositeDiscountRule : IDiscountRule
{
    private readonly List<IDiscountRule> _rules = new();

    public void AddRule(IDiscountRule rule) => _rules.Add(rule);

    public decimal Calculate(Order order)
    {
        return _rules.Sum(rule => rule.Calculate(order));
    }
}
```

**Services**: Search (Query parsing), Orders (Discount rules), Catalog (Filter expressions)

---

### 16. Iterator

**Purpose**: Provide a way to access elements of an aggregate object sequentially without exposing its underlying representation.

**Usage in R2.ShopNet**: Paginated product listing, Order history navigation

```csharp
namespace R2.ShopNet.Catalog.Domain.Iterators;

// Iterator Interface
public interface IProductIterator
{
    bool HasNext();
    Product Next();
    void Reset();
}

// Aggregate Interface
public interface IProductCollection
{
    IProductIterator CreateIterator();
    void Add(Product product);
    int Count { get; }
}

// Concrete Iterator
public class ProductIterator : IProductIterator
{
    private readonly List<Product> _products;
    private int _position = 0;

    public ProductIterator(List<Product> products)
    {
        _products = products;
    }

    public bool HasNext()
    {
        return _position < _products.Count;
    }

    public Product Next()
    {
        if (!HasNext())
        {
            throw new InvalidOperationException("No more products");
        }

        return _products[_position++];
    }

    public void Reset()
    {
        _position = 0;
    }
}

// Concrete Aggregate
public class ProductCollection : IProductCollection
{
    private readonly List<Product> _products = new();

    public void Add(Product product)
    {
        _products.Add(product);
    }

    public int Count => _products.Count;

    public IProductIterator CreateIterator()
    {
        return new ProductIterator(_products);
    }
}

// Filtered Iterator
public class FilteredProductIterator : IProductIterator
{
    private readonly List<Product> _products;
    private readonly Func<Product, bool> _filter;
    private int _position = 0;

    public FilteredProductIterator(List<Product> products, Func<Product, bool> filter)
    {
        _products = products.Where(filter).ToList();
    }

    public bool HasNext() => _position < _products.Count;
    public Product Next() => _products[_position++];
    public void Reset() => _position = 0;
}

// Usage
var collection = new ProductCollection();
collection.Add(new Product { Name = "Laptop", Price = 999 });
collection.Add(new Product { Name = "Mouse", Price = 29 });
collection.Add(new Product { Name = "Keyboard", Price = 79 });

var iterator = collection.CreateIterator();
while (iterator.HasNext())
{
    var product = iterator.Next();
    Console.WriteLine($"{product.Name}: ${product.Price}");
}

// With filter
var expensiveIterator = new FilteredProductIterator(
    products,
    p => p.Price > 100);
```

**Modern C# approach with IEnumerable:**

```csharp
public class ProductCollection : IEnumerable<Product>
{
    private readonly List<Product> _products = new();

    public void Add(Product product) => _products.Add(product);

    public IEnumerator<Product> GetEnumerator()
    {
        foreach (var product in _products)
        {
            yield return product;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // Custom iterator with filtering
    public IEnumerable<Product> GetProductsInPriceRange(decimal min, decimal max)
    {
        foreach (var product in _products)
        {
            if (product.Price >= min && product.Price <= max)
            {
                yield return product;
            }
        }
    }
}

// Usage with LINQ
foreach (var product in collection.Where(p => p.Price > 100))
{
    Console.WriteLine(product.Name);
}
```

**Services**: Catalog (Product pagination), Orders (Order history), Inventory (Stock listing)

---

### 17. Mediator

**Purpose**: Define an object that encapsulates how a set of objects interact. Promotes loose coupling by keeping objects from referring to each other explicitly.

**Usage in R2.ShopNet**: CQRS Mediator, Event Bus, Order processing coordination

```csharp
namespace R2.ShopNet.CQRS;

// Mediator Interface
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken);
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification;
}

// Request/Response
public interface IRequest<TResponse> { }

public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

// Notification
public interface INotification { }

public interface INotificationHandler<TNotification> where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}

// Concrete Mediator
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        var handlerType = typeof(IRequestHandler<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));

        dynamic handler = _serviceProvider.GetRequiredService(handlerType);

        return await handler.HandleAsync((dynamic)request, cancellationToken);
    }

    public async Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var handlerType = typeof(INotificationHandler<>)
            .MakeGenericType(typeof(TNotification));

        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (dynamic handler in handlers)
        {
            await handler.HandleAsync((dynamic)notification, cancellationToken);
        }
    }
}

// Example: Order Created Notification with multiple handlers
public class OrderCreatedNotification : INotification
{
    public Order Order { get; set; }
}

// Handler 1: Reserve Inventory
public class ReserveInventoryHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly IInventoryService _inventoryService;

    public async Task HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        foreach (var item in notification.Order.Items)
        {
            await _inventoryService.ReserveStockAsync(item.ProductId, item.Quantity);
        }
    }
}

// Handler 2: Send Email
public class SendOrderConfirmationEmailHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly IEmailService _emailService;

    public async Task HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _emailService.SendOrderConfirmationAsync(notification.Order);
    }
}

// Handler 3: Update Analytics
public class UpdateOrderAnalyticsHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly IAnalyticsService _analyticsService;

    public async Task HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _analyticsService.RecordOrderAsync(notification.Order);
    }
}

// Usage in Order Service
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IMediator _mediator;
    private readonly IOrderRepository _repository;

    public async Task<Result<OrderDto>> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var order = new Order { /* ... */ };
        await _repository.AddAsync(order, cancellationToken);

        // Publish notification - mediator coordinates all handlers
        await _mediator.PublishAsync(new OrderCreatedNotification { Order = order }, cancellationToken);

        // Manual mapping to DTO
        var dto = new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            Total = order.Total,
            Status = order.Status.ToString()
        };

        return Result<OrderDto>.Success(dto);
    }
}
```

**Services**: All services (CQRS coordination), Orders (Order processing), Common (Event bus)

---

### 18. Memento

**Purpose**: Capture and externalize an object's internal state so it can be restored later without violating encapsulation.

**Usage in R2.ShopNet**: Order draft saving, Cart state persistence, Product version history

```csharp
namespace R2.ShopNet.Orders.Domain.Mementos;

// Memento - Stores state
public class OrderMemento
{
    public int OrderId { get; private set; }
    public List<OrderItem> Items { get; private set; }
    public Address ShippingAddress { get; private set; }
    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime SavedAt { get; private set; }

    public OrderMemento(int orderId, List<OrderItem> items, Address shippingAddress,
        decimal total, OrderStatus status)
    {
        OrderId = orderId;
        Items = items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();
        ShippingAddress = shippingAddress;
        Total = total;
        Status = status;
        SavedAt = DateTime.UtcNow;
    }
}

// Originator - Creates and restores from memento
public class Order
{
    public int Id { get; set; }
    public List<OrderItem> Items { get; set; }
    public Address ShippingAddress { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }

    // Create memento
    public OrderMemento SaveState()
    {
        return new OrderMemento(Id, Items, ShippingAddress, Total, Status);
    }

    // Restore from memento
    public void RestoreState(OrderMemento memento)
    {
        Id = memento.OrderId;
        Items = memento.Items;
        ShippingAddress = memento.ShippingAddress;
        Total = memento.Total;
        Status = memento.Status;
    }
}

// Caretaker - Manages mementos
public class OrderHistory
{
    private readonly Stack<OrderMemento> _history = new();
    private readonly Stack<OrderMemento> _redoHistory = new();

    public void SaveState(Order order)
    {
        _history.Push(order.SaveState());
        _redoHistory.Clear(); // Clear redo history on new save
    }

    public void Undo(Order order)
    {
        if (_history.Count > 0)
        {
            _redoHistory.Push(order.SaveState()); // Save current for redo
            var memento = _history.Pop();
            order.RestoreState(memento);
        }
    }

    public void Redo(Order order)
    {
        if (_redoHistory.Count > 0)
        {
            _history.Push(order.SaveState()); // Save current for undo
            var memento = _redoHistory.Pop();
            order.RestoreState(memento);
        }
    }

    public bool CanUndo => _history.Count > 0;
    public bool CanRedo => _redoHistory.Count > 0;
}

// Usage
var order = new Order
{
    Items = new List<OrderItem>
    {
        new() { ProductId = 1, Quantity = 2, UnitPrice = 29.99m }
    }
};

var history = new OrderHistory();

// Save initial state
history.SaveState(order);

// Modify order
order.Items.Add(new OrderItem { ProductId = 2, Quantity = 1, UnitPrice = 49.99m });
history.SaveState(order);

// Modify again
order.Items.Add(new OrderItem { ProductId = 3, Quantity = 1, UnitPrice = 19.99m });
history.SaveState(order);

// Undo last change
if (history.CanUndo)
{
    history.Undo(order);
    Console.WriteLine($"After undo: {order.Items.Count} items");
}

// Redo
if (history.CanRedo)
{
    history.Redo(order);
    Console.WriteLine($"After redo: {order.Items.Count} items");
}
```

**Another example: Cart Draft**

```csharp
public class CartMemento
{
    public List<CartItem> Items { get; }
    public string CouponCode { get; }
    public DateTime SavedAt { get; }

    public CartMemento(List<CartItem> items, string couponCode)
    {
        Items = items.Select(i => new CartItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            Price = i.Price
        }).ToList();
        CouponCode = couponCode;
        SavedAt = DateTime.UtcNow;
    }
}

public class Cart
{
    public List<CartItem> Items { get; set; } = new();
    public string CouponCode { get; set; }

    public CartMemento SaveDraft() => new CartMemento(Items, CouponCode);

    public void LoadDraft(CartMemento memento)
    {
        Items = memento.Items;
        CouponCode = memento.CouponCode;
    }
}

// Save cart as draft
var cartDraft = cart.SaveDraft();
await _cacheService.SetAsync($"cart_draft_{userId}", cartDraft, TimeSpan.FromDays(7));

// Restore cart from draft
var savedDraft = await _cacheService.GetAsync<CartMemento>($"cart_draft_{userId}");
cart.LoadDraft(savedDraft);
```

**Services**: Orders (Order drafts), Cart (Save for later), Catalog (Product versions)

---

### 19. Observer

**Purpose**: Define a one-to-many dependency between objects so that when one object changes state, all its dependents are notified automatically.

**Usage in R2.ShopNet**: Real-time notifications, Stock alerts, Order status updates

```csharp
namespace R2.ShopNet.Common.Patterns;

// Subject Interface
public interface ISubject<T>
{
    void Attach(IObserver<T> observer);
    void Detach(IObserver<T> observer);
    void Notify(T data);
}

// Observer Interface
public interface IObserver<T>
{
    Task UpdateAsync(T data);
}

// Concrete Subject - Stock Monitor
public class StockMonitor : ISubject<StockChangedEvent>
{
    private readonly List<IObserver<StockChangedEvent>> _observers = new();
    private readonly Dictionary<int, int> _stockLevels = new();

    public void Attach(IObserver<StockChangedEvent> observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
    }

    public void Detach(IObserver<StockChangedEvent> observer)
    {
        _observers.Remove(observer);
    }

    public void Notify(StockChangedEvent data)
    {
        foreach (var observer in _observers)
        {
            _ = observer.UpdateAsync(data); // Fire and forget
        }
    }

    public void UpdateStock(int productId, int newQuantity)
    {
        var oldQuantity = _stockLevels.GetValueOrDefault(productId, 0);
        _stockLevels[productId] = newQuantity;

        // Notify observers
        Notify(new StockChangedEvent
        {
            ProductId = productId,
            OldQuantity = oldQuantity,
            NewQuantity = newQuantity,
            Timestamp = DateTime.UtcNow
        });
    }
}

// Event Data
public class StockChangedEvent
{
    public int ProductId { get; set; }
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsLowStock => NewQuantity < 10;
}

// Concrete Observer - Low Stock Alert
public class LowStockAlertObserver : IObserver<StockChangedEvent>
{
    private readonly INotificationService _notificationService;

    public async Task UpdateAsync(StockChangedEvent data)
    {
        if (data.IsLowStock && data.OldQuantity >= 10)
        {
            await _notificationService.SendAsync(new Notification
            {
                Type = NotificationType.LowStock,
                Message = $"Product {data.ProductId} is low on stock: {data.NewQuantity} remaining"
            });
        }
    }
}

// Concrete Observer - Cache Invalidation
public class CacheInvalidationObserver : IObserver<StockChangedEvent>
{
    private readonly ICacheManager _cacheManager;

    public async Task UpdateAsync(StockChangedEvent data)
    {
        await _cacheManager.RemoveAsync($"product_{data.ProductId}");
        await _cacheManager.RemoveAsync($"product_availability_{data.ProductId}");
    }
}

// Concrete Observer - Search Index Update
public class SearchIndexObserver : IObserver<StockChangedEvent>
{
    private readonly ISearchIndexer _searchIndexer;

    public async Task UpdateAsync(StockChangedEvent data)
    {
        await _searchIndexer.UpdateProductAvailabilityAsync(
            data.ProductId,
            data.NewQuantity > 0);
    }
}

// Usage
var stockMonitor = new StockMonitor();

// Attach observers
stockMonitor.Attach(new LowStockAlertObserver(notificationService));
stockMonitor.Attach(new CacheInvalidationObserver(cacheManager));
stockMonitor.Attach(new SearchIndexObserver(searchIndexer));

// Update stock - all observers notified automatically
stockMonitor.UpdateStock(productId: 123, newQuantity: 5);
```

**Modern .NET approach with Events:**

```csharp
public class StockService
{
    // Define event
    public event EventHandler<StockChangedEventArgs> StockChanged;

    protected virtual void OnStockChanged(StockChangedEventArgs e)
    {
        StockChanged?.Invoke(this, e);
    }

    public async Task UpdateStockAsync(int productId, int newQuantity)
    {
        // Update stock in database
        await _repository.UpdateStockAsync(productId, newQuantity);

        // Raise event
        OnStockChanged(new StockChangedEventArgs
        {
            ProductId = productId,
            NewQuantity = newQuantity
        });
    }
}

// Subscribe to event
stockService.StockChanged += async (sender, e) =>
{
    if (e.NewQuantity < 10)
    {
        await _notificationService.SendLowStockAlertAsync(e.ProductId);
    }
};
```

**Services**: Inventory (Stock monitoring), Orders (Status updates), Delivery (Location tracking)

---

### 20. State

**Purpose**: Allow an object to alter its behavior when its internal state changes.

**Usage in R2.ShopNet**: Order status management, Delivery status, Payment processing

```csharp
namespace R2.ShopNet.Orders.Domain.States;

// Context
public class Order
{
    public int Id { get; set; }
    public List<OrderItem> Items { get; set; }
    public OrderState State { get; set; }

    public Order()
    {
        State = new PendingState();
    }

    public void SetState(OrderState state)
    {
        State = state;
        State.SetContext(this);
    }

    // Delegate to state
    public async Task ProcessAsync() => await State.ProcessAsync();
    public async Task CancelAsync() => await State.CancelAsync();
    public async Task CompleteAsync() => await State.CompleteAsync();
}

// State base class
public abstract class OrderState
{
    protected Order _order;

    public void SetContext(Order order)
    {
        _order = order;
    }

    public abstract Task ProcessAsync();
    public abstract Task CancelAsync();
    public abstract Task CompleteAsync();
    public abstract string GetStatusName();
}

// Concrete States
public class PendingState : OrderState
{
    public override async Task ProcessAsync()
    {
        Console.WriteLine("Processing payment...");
        // Process payment
        _order.SetState(new ProcessingState());
    }

    public override async Task CancelAsync()
    {
        Console.WriteLine("Cancelling pending order...");
        _order.SetState(new CancelledState());
    }

    public override async Task CompleteAsync()
    {
        throw new InvalidOperationException("Cannot complete a pending order");
    }

    public override string GetStatusName() => "Pending";
}

public class ProcessingState : OrderState
{
    public override async Task ProcessAsync()
    {
        Console.WriteLine("Order is being processed in warehouse...");
        // Warehouse processing
        _order.SetState(new ShippedState());
    }

    public override async Task CancelAsync()
    {
        Console.WriteLine("Cancelling order and refunding payment...");
        // Refund logic
        _order.SetState(new CancelledState());
    }

    public override async Task CompleteAsync()
    {
        throw new InvalidOperationException("Cannot complete a processing order");
    }

    public override string GetStatusName() => "Processing";
}

public class ShippedState : OrderState
{
    public override async Task ProcessAsync()
    {
        throw new InvalidOperationException("Order already shipped");
    }

    public override async Task CancelAsync()
    {
        throw new InvalidOperationException("Cannot cancel shipped order");
    }

    public override async Task CompleteAsync()
    {
        Console.WriteLine("Order delivered successfully");
        _order.SetState(new DeliveredState());
    }

    public override string GetStatusName() => "Shipped";
}

public class DeliveredState : OrderState
{
    public override async Task ProcessAsync()
    {
        throw new InvalidOperationException("Order already delivered");
    }

    public override async Task CancelAsync()
    {
        Console.WriteLine("Initiating return process...");
        _order.SetState(new ReturnedState());
    }

    public override async Task CompleteAsync()
    {
        Console.WriteLine("Order already completed");
    }

    public override string GetStatusName() => "Delivered";
}

public class CancelledState : OrderState
{
    public override async Task ProcessAsync()
    {
        throw new InvalidOperationException("Cannot process cancelled order");
    }

    public override async Task CancelAsync()
    {
        Console.WriteLine("Order already cancelled");
    }

    public override async Task CompleteAsync()
    {
        throw new InvalidOperationException("Cannot complete cancelled order");
    }

    public override string GetStatusName() => "Cancelled";
}

public class ReturnedState : OrderState
{
    public override async Task ProcessAsync()
    {
        Console.WriteLine("Processing return and refund...");
    }

    public override async Task CancelAsync()
    {
        throw new InvalidOperationException("Cannot cancel returned order");
    }

    public override async Task CompleteAsync()
    {
        Console.WriteLine("Return completed");
    }

    public override string GetStatusName() => "Returned";
}

// Usage
var order = new Order { Id = 123 };

// Pending -> Processing
await order.ProcessAsync();
Console.WriteLine($"Status: {order.State.GetStatusName()}");

// Processing -> Shipped
await order.ProcessAsync();
Console.WriteLine($"Status: {order.State.GetStatusName()}");

// Shipped -> Delivered
await order.CompleteAsync();
Console.WriteLine($"Status: {order.State.GetStatusName()}");
```

**Services**: Orders (Order lifecycle), Delivery (Delivery status), Payment (Payment processing)

---

### 21. Strategy

**Purpose**: Define a family of algorithms, encapsulate each one, and make them interchangeable.

**Usage in R2.ShopNet**: Payment methods, Shipping calculators, Discount strategies, Search algorithms

```csharp
namespace R2.ShopNet.Orders.Domain.Strategies;

// Strategy Interface
public interface IShippingStrategy
{
    decimal CalculateShippingCost(Order order, Address destination);
    TimeSpan EstimateDeliveryTime(Address origin, Address destination);
    string GetShippingMethod();
}

// Concrete Strategies
public class StandardShippingStrategy : IShippingStrategy
{
    public decimal CalculateShippingCost(Order order, Address destination)
    {
        var weight = order.Items.Sum(i => i.Weight * i.Quantity);
        return 5.00m + (weight * 0.10m); // $5 base + $0.10 per pound
    }

    public TimeSpan EstimateDeliveryTime(Address origin, Address destination)
    {
        var distance = CalculateDistance(origin, destination);
        return TimeSpan.FromDays(5 + (distance / 500)); // 5 days + 1 day per 500 miles
    }

    public string GetShippingMethod() => "Standard Shipping";

    private double CalculateDistance(Address from, Address to)
    {
        // Simplified distance calculation
        return 1000; // miles
    }
}

public class ExpressShippingStrategy : IShippingStrategy
{
    public decimal CalculateShippingCost(Order order, Address destination)
    {
        var weight = order.Items.Sum(i => i.Weight * i.Quantity);
        return 15.00m + (weight * 0.20m); // Higher rates
    }

    public TimeSpan EstimateDeliveryTime(Address origin, Address destination)
    {
        var distance = CalculateDistance(origin, destination);
        return TimeSpan.FromDays(2); // Fixed 2 days
    }

    public string GetShippingMethod() => "Express Shipping";

    private double CalculateDistance(Address from, Address to) => 1000;
}

public class SameDayShippingStrategy : IShippingStrategy
{
    public decimal CalculateShippingCost(Order order, Address destination)
    {
        var weight = order.Items.Sum(i => i.Weight * i.Quantity);
        var baseCost = 25.00m + (weight * 0.30m);

        // Premium for same day
        var orderTime = DateTime.Now.Hour;
        if (orderTime > 12) // After noon
        {
            baseCost *= 1.5m; // 50% premium
        }

        return baseCost;
    }

    public TimeSpan EstimateDeliveryTime(Address origin, Address destination)
    {
        return TimeSpan.FromHours(6); // Same day
    }

    public string GetShippingMethod() => "Same-Day Delivery";
}

public class FreeShippingStrategy : IShippingStrategy
{
    private readonly decimal _minimumOrderAmount;

    public FreeShippingStrategy(decimal minimumOrderAmount = 50.00m)
    {
        _minimumOrderAmount = minimumOrderAmount;
    }

    public decimal CalculateShippingCost(Order order, Address destination)
    {
        return order.SubTotal >= _minimumOrderAmount ? 0m : 5.00m;
    }

    public TimeSpan EstimateDeliveryTime(Address origin, Address destination)
    {
        return TimeSpan.FromDays(7); // Standard time
    }

    public string GetShippingMethod() => "Free Shipping";
}

// Context
public class ShippingCalculator
{
    private IShippingStrategy _strategy;

    public ShippingCalculator(IShippingStrategy strategy)
    {
        _strategy = strategy;
    }

    public void SetStrategy(IShippingStrategy strategy)
    {
        _strategy = strategy;
    }

    public ShippingQuote GetQuote(Order order, Address destination)
    {
        return new ShippingQuote
        {
            Method = _strategy.GetShippingMethod(),
            Cost = _strategy.CalculateShippingCost(order, destination),
            EstimatedDelivery = DateTime.Now.Add(
                _strategy.EstimateDeliveryTime(order.WarehouseAddress, destination))
        };
    }
}

// Usage
var order = new Order { SubTotal = 100.00m };
var destination = new Address { /* ... */ };

var calculator = new ShippingCalculator(new StandardShippingStrategy());
var quote = calculator.GetQuote(order, destination);
Console.WriteLine($"{quote.Method}: ${quote.Cost} - Arrives {quote.EstimatedDelivery:d}");

// Change strategy dynamically
calculator.SetStrategy(new ExpressShippingStrategy());
quote = calculator.GetQuote(order, destination);
Console.WriteLine($"{quote.Method}: ${quote.Cost} - Arrives {quote.EstimatedDelivery:d}");
```

**Another example: Payment Strategy**

```csharp
public interface IPaymentStrategy
{
    Task<PaymentResult> ProcessPaymentAsync(decimal amount, PaymentDetails details);
    string GetPaymentMethod();
}

public class CreditCardPaymentStrategy : IPaymentStrategy
{
    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, PaymentDetails details)
    {
        // Process credit card payment
        return new PaymentResult { Success = true, TransactionId = Guid.NewGuid().ToString() };
    }

    public string GetPaymentMethod() => "Credit Card";
}

public class PayPalPaymentStrategy : IPaymentStrategy
{
    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, PaymentDetails details)
    {
        // Process PayPal payment
        return new PaymentResult { Success = true, TransactionId = Guid.NewGuid().ToString() };
    }

    public string GetPaymentMethod() => "PayPal";
}

public class CashOnDeliveryStrategy : IPaymentStrategy
{
    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, PaymentDetails details)
    {
        // No immediate payment processing
        return new PaymentResult { Success = true, TransactionId = "COD-" + Guid.NewGuid() };
    }

    public string GetPaymentMethod() => "Cash on Delivery";
}

// Payment Processor Context
public class PaymentProcessor
{
    public async Task<PaymentResult> ProcessAsync(Order order, IPaymentStrategy strategy)
    {
        Console.WriteLine($"Processing payment via {strategy.GetPaymentMethod()}");
        return await strategy.ProcessPaymentAsync(order.Total, order.PaymentDetails);
    }
}
```

**Another example: Search Strategy**

```csharp
public interface ISearchStrategy
{
    Task<List<Product>> SearchAsync(string query, SearchFilters filters);
}

public class ElasticsearchStrategy : ISearchStrategy
{
    public async Task<List<Product>> SearchAsync(string query, SearchFilters filters)
    {
        // Elasticsearch full-text search
        return await _elasticClient.SearchAsync<Product>(s => s
            .Query(q => q.Match(m => m.Field(f => f.Name).Query(query)))
            .Filter(/* apply filters */)
        );
    }
}

public class DatabaseSearchStrategy : ISearchStrategy
{
    public async Task<List<Product>> SearchAsync(string query, SearchFilters filters)
    {
        // SQL LIKE search (fallback)
        return await _context.Products
            .Where(p => p.Name.Contains(query) || p.Description.Contains(query))
            .ToListAsync();
    }
}
```

**Services**: Orders (Shipping calculation), Payment (Payment processing), Search (Search algorithms), Catalog (Pricing strategies)

---

### 22. Template Method

**Purpose**: Define the skeleton of an algorithm, deferring some steps to subclasses.

**Usage in R2.ShopNet**: Order processing workflow, Report generation, Import/Export processes

```csharp
namespace R2.ShopNet.Orders.Domain.Templates;

// Abstract Class - Template Method
public abstract class OrderProcessor
{
    // Template Method - defines skeleton
    public async Task<OrderResult> ProcessOrderAsync(Order order)
    {
        try
        {
            // Step 1: Validate
            var validationResult = await ValidateOrderAsync(order);
            if (!validationResult.IsValid)
            {
                return OrderResult.Failure(validationResult.Error);
            }

            // Step 2: Reserve inventory
            await ReserveInventoryAsync(order);

            // Step 3: Calculate totals (hook method - can be overridden)
            CalculateTotals(order);

            // Step 4: Process payment
            var paymentResult = await ProcessPaymentAsync(order);
            if (!paymentResult.IsSuccess)
            {
                await ReleaseInventoryAsync(order); // Rollback
                return OrderResult.Failure(paymentResult.Error);
            }

            // Step 5: Create order record
            await SaveOrderAsync(order);

            // Step 6: Send notifications (hook method)
            await SendNotificationsAsync(order);

            // Step 7: Additional processing (hook method - optional)
            await PerformAdditionalProcessingAsync(order);

            return OrderResult.Success(order);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(order, ex);
            throw;
        }
    }

    // Abstract methods - must be implemented by subclasses
    protected abstract Task<ValidationResult> ValidateOrderAsync(Order order);
    protected abstract Task ProcessPaymentAsync(Order order);

    // Concrete methods - common to all
    protected async Task ReserveInventoryAsync(Order order)
    {
        foreach (var item in order.Items)
        {
            await _inventoryService.ReserveStockAsync(item.ProductId, item.Quantity);
        }
    }

    protected async Task ReleaseInventoryAsync(Order order)
    {
        foreach (var item in order.Items)
        {
            await _inventoryService.ReleaseStockAsync(item.ProductId, item.Quantity);
        }
    }

    protected async Task SaveOrderAsync(Order order)
    {
        await _orderRepository.AddAsync(order);
    }

    // Hook methods - can be overridden but have default behavior
    protected virtual void CalculateTotals(Order order)
    {
        order.SubTotal = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        order.Tax = order.SubTotal * 0.08m; // 8% tax
        order.Total = order.SubTotal + order.Tax + order.ShippingCost;
    }

    protected virtual async Task SendNotificationsAsync(Order order)
    {
        await _notificationService.SendOrderConfirmationAsync(order);
    }

    protected virtual async Task PerformAdditionalProcessingAsync(Order order)
    {
        // Default: do nothing
        await Task.CompletedTask;
    }

    protected virtual async Task HandleErrorAsync(Order order, Exception ex)
    {
        await _logger.LogErrorAsync($"Order processing failed: {ex.Message}");
    }
}

// Concrete Class - B2C Order Processor
public class B2COrderProcessor : OrderProcessor
{
    protected override async Task<ValidationResult> ValidateOrderAsync(Order order)
    {
        // B2C validation rules
        if (order.Items.Count == 0)
            return ValidationResult.Failure("Order must have at least one item");

        if (order.Total < 5.00m)
            return ValidationResult.Failure("Minimum order amount is $5");

        return ValidationResult.Success();
    }

    protected override async Task ProcessPaymentAsync(Order order)
    {
        // Process credit card or PayPal
        return await _paymentGateway.ChargeAsync(order.PaymentInfo, order.Total);
    }

    protected override async Task SendNotificationsAsync(Order order)
    {
        await base.SendNotificationsAsync(order); // Call base
        // Additional B2C notifications
        await _smsService.SendOrderConfirmationAsync(order.CustomerPhone);
    }
}

// Concrete Class - B2B Order Processor
public class B2BOrderProcessor : OrderProcessor
{
    protected override async Task<ValidationResult> ValidateOrderAsync(Order order)
    {
        // B2B validation rules
        if (order.Items.Count == 0)
            return ValidationResult.Failure("Order must have at least one item");

        if (order.Total < 100.00m)
            return ValidationResult.Failure("Minimum B2B order amount is $100");

        // Check credit limit
        var creditLimit = await _creditService.GetCreditLimitAsync(order.CustomerId);
        if (order.Total > creditLimit)
            return ValidationResult.Failure("Order exceeds credit limit");

        return ValidationResult.Success();
    }

    protected override async Task ProcessPaymentAsync(Order order)
    {
        // B2B uses invoice payment
        await _invoiceService.CreateInvoiceAsync(order);
        return PaymentResult.Success("Invoice created");
    }

    protected override void CalculateTotals(Order order)
    {
        order.SubTotal = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        // B2B discount
        if (order.SubTotal > 1000)
        {
            order.Discount = order.SubTotal * 0.10m; // 10% bulk discount
        }

        order.Tax = (order.SubTotal - order.Discount) * 0.08m;
        order.Total = order.SubTotal - order.Discount + order.Tax + order.ShippingCost;
    }

    protected override async Task PerformAdditionalProcessingAsync(Order order)
    {
        // Create purchase order document
        await _documentService.GeneratePurchaseOrderAsync(order);

        // Notify account manager
        await _notificationService.NotifyAccountManagerAsync(order);
    }
}

// Usage
OrderProcessor processor = customerType == CustomerType.B2C
    ? new B2COrderProcessor()
    : new B2BOrderProcessor();

var result = await processor.ProcessOrderAsync(order);
```

**Another example: Report Generation**

```csharp
public abstract class ReportGenerator
{
    // Template method
    public async Task<Report> GenerateReportAsync(ReportRequest request)
    {
        var data = await FetchDataAsync(request);
        var processedData = ProcessData(data);
        var formattedReport = FormatReport(processedData);
        await SaveReportAsync(formattedReport);
        return formattedReport;
    }

    protected abstract Task<RawData> FetchDataAsync(ReportRequest request);
    protected abstract ProcessedData ProcessData(RawData data);
    protected abstract Report FormatReport(ProcessedData data);

    protected virtual async Task SaveReportAsync(Report report)
    {
        await _reportRepository.SaveAsync(report);
    }
}

public class SalesReportGenerator : ReportGenerator
{
    protected override async Task<RawData> FetchDataAsync(ReportRequest request)
    {
        return await _orderRepository.GetSalesDataAsync(request.StartDate, request.EndDate);
    }

    protected override ProcessedData ProcessData(RawData data)
    {
        // Calculate totals, averages, etc.
        return new ProcessedData { /* ... */ };
    }

    protected override Report FormatReport(ProcessedData data)
    {
        return new Report
        {
            Title = "Sales Report",
            Sections = CreateSalesSections(data)
        };
    }
}
```

**Services**: Orders (Order processing), Analytics (Report generation), Warehouse (Fulfillment process)

---

### 23. Visitor

**Purpose**: Represent an operation to be performed on elements of an object structure.

**Usage in R2.ShopNet**: Order item calculations, Tax calculations, Discount application

```csharp
namespace R2.ShopNet.Orders.Domain.Visitors;

// Element Interface
public interface IOrderComponent
{
    void Accept(IOrderVisitor visitor);
}

// Visitor Interface
public interface IOrderVisitor
{
    void Visit(Order order);
    void Visit(OrderItem item);
    void Visit(ShippingInfo shipping);
    void Visit(DiscountInfo discount);
}

// Concrete Elements
public class Order : IOrderComponent
{
    public int Id { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public ShippingInfo Shipping { get; set; }
    public DiscountInfo Discount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public void Accept(IOrderVisitor visitor)
    {
        visitor.Visit(this);

        foreach (var item in Items)
        {
            item.Accept(visitor);
        }

        Shipping?.Accept(visitor);
        Discount?.Accept(visitor);
    }
}

public class OrderItem : IOrderComponent
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public bool IsTaxable { get; set; } = true;

    public void Accept(IOrderVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class ShippingInfo : IOrderComponent
{
    public string Method { get; set; }
    public decimal Cost { get; set; }
    public bool IsTaxable { get; set; } = false;

    public void Accept(IOrderVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class DiscountInfo : IOrderComponent
{
    public string Code { get; set; }
    public decimal Amount { get; set; }
    public DiscountType Type { get; set; }

    public void Accept(IOrderVisitor visitor)
    {
        visitor.Visit(this);
    }
}

// Concrete Visitor - Price Calculator
public class PriceCalculatorVisitor : IOrderVisitor
{
    private decimal _total = 0;

    public decimal GetTotal() => _total;

    public void Visit(Order order)
    {
        _total = 0; // Reset
    }

    public void Visit(OrderItem item)
    {
        item.Total = item.Quantity * item.UnitPrice;
        _total += item.Total;
    }

    public void Visit(ShippingInfo shipping)
    {
        _total += shipping.Cost;
    }

    public void Visit(DiscountInfo discount)
    {
        if (discount.Type == DiscountType.Percentage)
        {
            _total -= _total * (discount.Amount / 100);
        }
        else
        {
            _total -= discount.Amount;
        }
    }
}

// Concrete Visitor - Tax Calculator
public class TaxCalculatorVisitor : IOrderVisitor
{
    private decimal _taxableAmount = 0;
    private const decimal TaxRate = 0.08m; // 8%

    public decimal GetTax() => _taxableAmount * TaxRate;

    public void Visit(Order order)
    {
        _taxableAmount = 0;
    }

    public void Visit(OrderItem item)
    {
        if (item.IsTaxable)
        {
            _taxableAmount += item.Total;
        }
    }

    public void Visit(ShippingInfo shipping)
    {
        if (shipping.IsTaxable)
        {
            _taxableAmount += shipping.Cost;
        }
    }

    public void Visit(DiscountInfo discount)
    {
        // Discounts don't affect tax calculation in this example
    }
}

// Concrete Visitor - Order Summary Generator
public class OrderSummaryVisitor : IOrderVisitor
{
    private readonly StringBuilder _summary = new();

    public string GetSummary() => _summary.ToString();

    public void Visit(Order order)
    {
        _summary.AppendLine($"Order #{order.Id}");
        _summary.AppendLine("Items:");
    }

    public void Visit(OrderItem item)
    {
        _summary.AppendLine($"  - {item.ProductName} x{item.Quantity} = ${item.Total:F2}");
    }

    public void Visit(ShippingInfo shipping)
    {
        _summary.AppendLine($"Shipping ({shipping.Method}): ${shipping.Cost:F2}");
    }

    public void Visit(DiscountInfo discount)
    {
        _summary.AppendLine($"Discount ({discount.Code}): -${discount.Amount:F2}");
    }
}

// Concrete Visitor - Invoice Generator
public class InvoiceGeneratorVisitor : IOrderVisitor
{
    private readonly Invoice _invoice = new();

    public Invoice GetInvoice() => _invoice;

    public void Visit(Order order)
    {
        _invoice.OrderId = order.Id;
        _invoice.Date = DateTime.UtcNow;
    }

    public void Visit(OrderItem item)
    {
        _invoice.LineItems.Add(new InvoiceLineItem
        {
            Description = item.ProductName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Total = item.Total
        });
    }

    public void Visit(ShippingInfo shipping)
    {
        _invoice.ShippingCost = shipping.Cost;
    }

    public void Visit(DiscountInfo discount)
    {
        _invoice.DiscountAmount = discount.Amount;
    }
}

// Usage
var order = new Order
{
    Id = 123,
    Items = new List<OrderItem>
    {
        new() { ProductName = "Laptop", Quantity = 1, UnitPrice = 999.99m, IsTaxable = true },
        new() { ProductName = "Mouse", Quantity = 2, UnitPrice = 29.99m, IsTaxable = true }
    },
    Shipping = new ShippingInfo { Method = "Express", Cost = 15.00m },
    Discount = new DiscountInfo { Code = "SAVE10", Amount = 10, Type = DiscountType.Percentage }
};

// Apply visitors
var priceCalculator = new PriceCalculatorVisitor();
order.Accept(priceCalculator);
var subtotal = priceCalculator.GetTotal();

var taxCalculator = new TaxCalculatorVisitor();
order.Accept(taxCalculator);
var tax = taxCalculator.GetTax();

order.SubTotal = subtotal;
order.Tax = tax;
order.Total = subtotal + tax;

// Generate summary
var summaryGenerator = new OrderSummaryVisitor();
order.Accept(summaryGenerator);
Console.WriteLine(summaryGenerator.GetSummary());

// Generate invoice
var invoiceGenerator = new InvoiceGeneratorVisitor();
order.Accept(invoiceGenerator);
var invoice = invoiceGenerator.GetInvoice();
```

**Services**: Orders (Price/Tax calculation), Analytics (Report generation), Catalog (Product export)

---

## Summary Table

| Pattern | Category | Primary Usage in R2.ShopNet | Services |
|---------|----------|------------------------------|----------|
| Abstract Factory | Creational | Payment gateway families | Payment, Notifications |
| Builder | Creational | Complex object construction | Orders, Catalog, Search |
| Factory Method | Creational | Repository/Service creation | All services |
| Prototype | Creational | Object cloning/duplication | Catalog, Cart |
| Singleton | Creational | Shared managers | Common, Infrastructure |
| Adapter | Structural | External API integration | Delivery, Payment |
| Bridge | Structural | Abstraction-implementation separation | Notifications |
| Composite | Structural | Tree structures | Catalog, Authorization |
| Decorator | Structural | Dynamic behavior addition | Orders, Common |
| Facade | Structural | Simplified complex interfaces | Orders (Checkout) |
| Flyweight | Structural | Memory optimization | Catalog, Delivery |
| Proxy | Structural | Access control, lazy loading | All services |
| Chain of Responsibility | Behavioral | Request handling pipeline | Orders, Payment |
| Command | Behavioral | Action encapsulation (CQRS) | All services |
| Interpreter | Behavioral | Language/rule parsing | Search, Orders |
| Iterator | Behavioral | Collection traversal | Catalog, Orders |
| Mediator | Behavioral | Component communication | All services (CQRS) |
| Memento | Behavioral | State preservation | Orders, Cart |
| Observer | Behavioral | Event notification | Inventory, Orders |
| State | Behavioral | State-dependent behavior | Orders, Delivery |
| Strategy | Behavioral | Algorithm selection | Orders, Payment, Search |
| Template Method | Behavioral | Algorithm skeleton | Orders, Analytics |
| Visitor | Behavioral | Operations on structures | Orders, Analytics |

---

**Document Version**: 1.0
**Last Updated**: 2025-10-17
**Maintained By**: Development Team
**Status**: Implementation Guide

**Note**: These patterns should be applied where they provide clear benefits. Don't force patterns where they're not needed - keep it simple (KISS principle) and only use patterns that solve real problems in your codebase.
