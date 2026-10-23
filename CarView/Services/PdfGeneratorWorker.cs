using CarView.Models;
using CloudinaryDotNet.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using CarView.Data;
using QuestPDF.Drawing;
using System.IO;

public class PdfGeneratorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory; 
    private IConnection _connection;
    private  RabbitMQ.Client.IModel _channel;

    public PdfGeneratorWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        QuestPDF.Settings.License = LicenseType.Community;

        string fontsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Fonts");

        string regularFont = Path.Combine(fontsFolder, "ARIAL.TTF");
        string boldFont = Path.Combine(fontsFolder, "ARIALBD.TTF");

        if (File.Exists(regularFont)) FontManager.RegisterFont(File.OpenRead(regularFont));
        if (File.Exists(boldFont)) FontManager.RegisterFont(File.OpenRead(boldFont));


        string rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";

        var factory = new ConnectionFactory() { HostName = rabbitHost };
        int retries = 5;
        while (retries > 0)
        {
            try
            {
                _connection = factory.CreateConnection();
                break; 
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
            {
                retries--;
                if (retries == 0) throw; 
                System.Threading.Thread.Sleep(3000); 
            }
        }
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(queue: "pdf_generation_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = JsonSerializer.Deserialize<JsonElement>(Encoding.UTF8.GetString(body));
            int carId = message.GetProperty("CarId").GetInt32();

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>(); 

                var car = await context.Cars
                    .Include(c => c.Brand)
                    .FirstOrDefaultAsync(c => c.Id == carId, stoppingToken);

                if (car != null)
                {
                    GenerateCarPdf(car);
                }
            }
            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(queue: "pdf_generation_queue", autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    private void GenerateCarPdf(Car car) 
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);

                page.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(12));

                page.Header()
                    .Text($"Паспорт автомобіля: {car.Brand?.Name} {car.ModelName}")
                    .SemiBold().FontSize(22).FontColor(Colors.Blue.Darken2);

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().PaddingBottom(10).Text("Технічні характеристики").FontSize(16).SemiBold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(150); 
                            columns.RelativeColumn();    
                        });

                        void AddRow(string label, string value)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(label).SemiBold();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(value ?? "Не вказано");
                        }

                        AddRow("Рік випуску", car.Year.ToString());
                        AddRow("Ціна", $"$ {car.Price:N0}");
                        AddRow("Кузов", car.BodyType.ToString());
                        AddRow("Трансмісія", car.Transmission.ToString());
                        AddRow("Тип пального", car.FuelType.ToString());
                        AddRow("Двигун", car.EngineCapacity);

                        if (car.HorsePower > 0)
                            AddRow("Потужність", $"{car.HorsePower} к.с.");

                        AddRow("Привід", car.Drivetrain);
                    });

                    col.Item().PaddingTop(20).PaddingBottom(5).Text("Опис").FontSize(16).SemiBold();
                    col.Item().Text(car.Description ?? "Опис відсутній.");
                });

                page.Footer().AlignCenter().Text(x => {
                    x.Span("Згенеровано асинхронно через RabbitMQ | CarView | Сторінка ");
                    x.CurrentPageNumber();
                });
            });
        });

        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "reports", $"Car_{car.Id}.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        document.GeneratePdf(filePath);
    }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}