using System;
using System.Text;
using System.Linq;
using System.Data.SqlTypes;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Microsoft.EntityFrameworkCore;
using MySQL.Data.EntityFrameworkCore.Extensions;

namespace sharp_start
{
	public class Player
	{
		public string Id { get; set; }
		//public Guid ID { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
	}

	public class AuthContext : DbContext
	{
		public DbSet<Player> Players { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseMySQL("server=mysql;database=auth;user=root;password=admin");
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<Player>(e =>
			{
				e.Property(k => k.Id).HasColumnType(typeName:"CHAR(36)");//HasAnnotation("Column", new { TypeName = "CHAR(36)" });
				e.HasKey(k => k.Id);
				e.HasKey(k => k.Email);
				e.Property(p => p.Password).IsRequired();
			});
		}
	}

	class Program
	{
		private static void Insert()
		{
			using (var c = new AuthContext())
			{
				c.Database.EnsureCreated();

				var p = new Player
				{
					Id = Guid.NewGuid().ToString(),
					Email = "test@test.com",
					Password = "pwd"
				};
				c.Players.Add(p);
				c.SaveChanges();
			}
		}
		
		private static Guid Auth(string email, string pwd)
		{
			using (var c = new AuthContext())
			{
				var id = from p in c.Players where p.Email == email && p.Password == pwd select p.Id;
				//id.AnyAsync CountAsync().ContinueWith( (t) =>
				return id.Any() ? Guid.Parse(id.First()) : Guid.Empty;
			}
		}

		static void Main(string[] args)
		{
			//Insert();
			Console.WriteLine("{0}", Auth("test@test.com", "pwd"));

			var factory = new ConnectionFactory() { HostName = "rabbit", UserName = "guest", Password = "guest" };

			using (var connection = factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				channel.ExchangeDeclare(exchange: "message", type: "topic");

				channel.QueueDeclare(queue: "test",
														durable: false,
														exclusive: false,
														autoDelete: false,
														arguments: null);

				channel.QueueBind(queue: "test",
													exchange: "message",
													routingKey: "pro");

				var consumer = new EventingBasicConsumer(channel);
				consumer.Received += (model, ea) =>
				{
					var content = ea.Body;
					var text = Encoding.UTF8.GetString(content);
					
					Console.WriteLine(" [x] Received {0} key {1}", text, ea.RoutingKey);
				};
				channel.BasicConsume(queue: "test",
														autoAck: true,
														consumer: consumer);

				Console.WriteLine(" Press [enter] to exit.");
				Console.ReadLine();
				
				// string message = "Hello World!";
				// var body = Encoding.UTF8.GetBytes(message);

				// channel.BasicPublish(exchange: "",
				// 										routingKey: "hello",
				// 										basicProperties: null,
				// 										body: body);
				// Console.WriteLine(" [x] Sent {0}", message);
			}
		}
	}
}
