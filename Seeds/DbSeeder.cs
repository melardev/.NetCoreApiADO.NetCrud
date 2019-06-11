using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ApiCoreAdoNetCrud.Entities;
using Bogus;
using Microsoft.Extensions.Configuration;

namespace ApiCoreAdoNetCrud.Seeds
{
    public class DbSeeder
    {
        private static string _connectionString;

        public static void Seed(IConfiguration configuration)
        {
            _connectionString = configuration["DataSources:SqlServer:ConnectionString"];
            SeedTodos();
            // SeedEntity2();
            // SeedEntity3();
            // ....
        }


        public static void SeedTodos()
        {
            int todosCount = GetTodoCount();
            int todosToSeed = 32;
            todosToSeed -= todosCount;
            if (todosToSeed > 0)
            {
                Console.WriteLine($"[+] Seeding {todosToSeed} Todos");
                var faker = new Faker<Todo>()
                    .RuleFor(a => a.Title, f => String.Join(" ", f.Lorem.Words(f.Random.Int(2, 5))))
                    .RuleFor(a => a.Description, f => f.Lorem.Sentences(f.Random.Int(min: 1, max: 10)))
                    .RuleFor(t => t.Completed, f => f.Random.Bool(0.4f))
                    .RuleFor(a => a.CreatedAt,
                        f => f.Date.Between(DateTime.Now.AddYears(-5), DateTime.Now.AddDays(-1)))
                    .FinishWith(async (f, todoInstance) =>
                    {
                        todoInstance.UpdatedAt =
                            f.Date.Between(start: todoInstance.CreatedAt, end: DateTime.Now);
                    });

                List<Todo> todos = faker.Generate(todosToSeed);
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string sql = $"Insert Into Todo (Title, Description, Completed, CreatedAt, UpdatedAt) Values " +
                                 $"(@Title, @Description, @Completed, @CreatedAt, @UpdatedAt)";

                    connection.Open();
                   
                    foreach (var todo in todos)
                    {
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            command.CommandType = CommandType.Text;
                            
                            command.Parameters.AddWithValue("Title", todo.Title);
                            command.Parameters.AddWithValue("Description", todo.Description);
                            command.Parameters.AddWithValue("Completed", todo.Completed);
                            command.Parameters.AddWithValue("CreatedAt", todo.CreatedAt);
                            command.Parameters.AddWithValue("UpdatedAt", todo.UpdatedAt);
                            command.ExecuteNonQuery();
                        }
                    }

                    connection.Close();
                }
            }
        }

        private static int GetTodoCount()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = "Select Id from Todo";
                    command.Connection = connection;

                    SqlDataAdapter dataAdapter = new SqlDataAdapter(command);

                    var dataTable = new DataTable("Todo");

                    int rowcount = dataAdapter.Fill(dataTable);
                    return rowcount;
                }
            }
        }
    }
}