using DocGenerator.Common;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

class Program
{

   static IConnection connection;
    private static readonly string createDocument = "create_document_queue";
    private static readonly string documentCreated = "document_created_queue";
    private static readonly string documentCreateExchange = "document_create_exchange";
    static IModel  _channel;
    static IModel channel => _channel ?? (_channel = GetChannel());
    static void Main(string[] args)
    {
        connection = GetConnection();


        channel.ExchangeDeclare(documentCreateExchange, "direct");
        channel.QueueDeclare(createDocument, false, false, false);
        channel.QueueBind(createDocument, documentCreateExchange, createDocument);

        channel.QueueDeclare(documentCreated, false, false, false);
        channel.QueueBind(documentCreated, documentCreateExchange, documentCreated);

        var consumerEvent = new EventingBasicConsumer(channel);

        consumerEvent.Received += (ch, ea) =>
        {
            var modelJson = Encoding.UTF8.GetString(ea.Body.ToArray());
            var model = JsonConvert.DeserializeObject<CreateDocumentModel>(modelJson);
            Console.WriteLine($"received data URL:{modelJson}");

            model.Url = "https://www.google.com/docs/x.pdf";

            WriteToQueue(documentCreated, model);

        };
        channel.BasicConsume(createDocument, true, consumerEvent);

        Console.WriteLine($"{documentCreateExchange} listening");
        Console.ReadLine();

    }

    private static void WriteToQueue(string queueName, CreateDocumentModel model)
    {
        var messageArr = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));

        channel.BasicPublish(documentCreateExchange, queueName, null, messageArr);
        Console.WriteLine("Message Published");
    }

    private static IModel GetChannel()
    {
        if (connection == null || !connection.IsOpen)
            connection = GetConnection();

        return connection.CreateModel();
    }

    private static IConnection GetConnection()
    {
        var connectionFactory = new ConnectionFactory()
        {
            Uri = new Uri("amqps://ivemkpwd:vODIFdwCnDAR8KvFc4WFi37fBEur-pgn@sparrow.rmq.cloudamqp.com/ivemkpwd")
        };
        return connectionFactory.CreateConnection();
    }
}