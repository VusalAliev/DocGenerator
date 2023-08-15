using DocGenerator.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Security.Principal;
using System.Text;

namespace DocGenerator.MVC.Controllers;
[Route("api/[controller]/[Action]")]
[ApiController]
public class GeneratorController : ControllerBase
{
    IConnection connection;
    private readonly string createDocument = "create_document_queue";
    private readonly string documentCreated = "document_created_queue";
    private readonly string documentCreateExchange = "document_create_exchange";
     IModel _channel;
    IModel channel => _channel ?? (_channel = GetChannel());

    [HttpPost]
    public IActionResult Connect()
    {
        if (connection == null || !connection.IsOpen)
            connection = GetConnection();

        channel.ExchangeDeclare(documentCreateExchange, "direct");
        channel.QueueDeclare(createDocument, false,false,false);
        channel.QueueBind(createDocument, documentCreateExchange, createDocument);

        channel.QueueDeclare(documentCreated,false,false,false);
        channel.QueueBind(documentCreated, documentCreateExchange, documentCreated);

        return Ok("Connection is open now");
    }
    
    [HttpPost]
    public IActionResult CreateDocument()
    {
        var model = new CreateDocumentModel()
        {
            UserId=1,
            DocumentType=DocumentType.Pdf
        };
        WriteToQueue(createDocument, model);

        var consumerEvent = new EventingBasicConsumer(channel);

        consumerEvent.Received += (ch, ea) =>
        {
            var modelStr=JsonConvert.DeserializeObject<CreateDocumentModel>(Encoding.UTF8.GetString(ea.Body.ToArray()));
            Console.WriteLine($"received data:{modelStr}");

        };
        channel.BasicConsume(documentCreated, true, consumerEvent);    


        return Ok("Message Created");
    }
    
    
    
    
    
    
    
    
    
    
    
    private void WriteToQueue(string queueName, CreateDocumentModel model)
    {
        var messageArr=Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));

        channel.BasicPublish(documentCreateExchange,queueName,null,messageArr);
        Console.WriteLine("Message Published");
    }

    private IModel GetChannel()
    {
        if (connection == null || !connection.IsOpen)
            connection = GetConnection();

        return connection.CreateModel();
    }

    private IConnection GetConnection()
    {
        var connectionFactory = new ConnectionFactory()
        {
            Uri = new Uri("amqps://ivemkpwd:vODIFdwCnDAR8KvFc4WFi37fBEur-pgn@sparrow.rmq.cloudamqp.com/ivemkpwd")
        };
        return connectionFactory.CreateConnection();
    }
}