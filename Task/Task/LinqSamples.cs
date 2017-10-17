// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SampleSupport;
using Task.Data;
using System.Text.RegularExpressions;

// Version Mad01

namespace SampleQueries
{
    [Title("LINQ Module")]
    [Prefix("Linq")]
    public class LinqSamples : SampleHarness
    {

        private DataSource dataSource = new DataSource();

        [Category("Where")]
        [Title("LINQ-001")]
        [Description("Клиенты, чей суммарный оборот превышает X.")]
        public void Linq001()
        {
            ShowResultByCustomerWithTurnover(10000);
            ShowResultByCustomerWithTurnover(50000);
            ShowResultByCustomerWithTurnover(100000);
        }

        [Category("Where")]
        [Title("LINQ-003")]
        [Description("Клиенты, у которых заказы превосходят по сумме величину X. И сумма всех заказов")]
        public void Linq003()
        {
            var amount = 1000;
            var customers = dataSource.Customers.Where(c => c.Orders.Any(o => o.Total > amount));

            Console.WriteLine($"Список Клиентов, чьи сумма заказов превосходит {amount}:");

            foreach (var customer in customers)
            {
                var order = customer.Orders.First(o => o.Total > amount);
                Console.WriteLine($"{customer.CustomerID}: Id Заказа: {order.OrderID} - Сумма заказа: {order.Total}");
            }

            Console.WriteLine();

            decimal totalAmount = 0;

            foreach (var customer in customers)
            {
                foreach (var order in customer.Orders)
                {
                    totalAmount += order.Total;
                }
            }

            Console.WriteLine($"Сумма всех заказов:{totalAmount}");
        }

        [Category("Where")]
        [Title("LINQ-006")]
        [Description("Показать клиентов с нецифровым кодом или без региона или в телефоне не указан код оператора (для простоты считаем, что это равнозначно «нет круглых скобочек в начале»).")]
        public void Linq006()
        {
            var customers = dataSource.Customers.Where(c => (!string.IsNullOrEmpty(c.Phone) && !c.Phone.StartsWith("(")));

            foreach (var customer in customers)
            {
                if (string.IsNullOrEmpty(customer.Region) || !string.IsNullOrEmpty(customer.PostalCode) && !customer.PostalCode.Contains(string.Format(@"\d")))
                {
                    Console.WriteLine($"{customer.CustomerID} - {customer.PostalCode} - {customer.Region} - {customer.Phone}");
                }
            }
        }

        [Category("Join")]
        [Title("LINQ-002")]
        [Description("Список поставщиков Клиента в одной локации для него.")]
        public void Linq002()
        {
            var withoutGroping = dataSource.Customers.Join(dataSource.Suppliers, c => new { c.City, c.Country },
                s => new { s.City, s.Country },
                (c, s) =>
                    new
                    {
                        CustomerId = c.CustomerID,
                        CustomerCity = c.City,
                        CustomerCountry = c.Country,
                        s.SupplierName
                    });

            foreach (var element in withoutGroping)
            {
                Console.WriteLine($"{element.CustomerId} - {element.CustomerCity} - {element.CustomerCountry} - {element.SupplierName}");
            }

            Console.WriteLine();

            var withGrouping = withoutGroping.GroupBy(e => new { e.CustomerCountry, e.CustomerCity });
            foreach (var element in withGrouping.SelectMany(@group => @group))
            {
                Console.WriteLine($"{element.CustomerCountry} - {element.CustomerCity} - {element.CustomerId} - {element.SupplierName}");
            }
        }

        [Category("OrderBy")]
        [Title("LINQ-004")]
        [Description("Список Клиентов с указанием даты, начиная с которой они стали Клиентами.")]
        public void Linq004()
        {
            foreach (var customer in dataSource.Customers)
            {
                var date = customer.Orders.OrderBy(o => o.OrderDate).FirstOrDefault()?.OrderDate;

                if (date != null)
                {
                    Console.WriteLine($"CustomerID: {customer.CustomerID} - Start date: {date}");
                }
            }
        }

        [Category("OrderBy")]
        [Title("LINQ-005")]
        [Description("Список Клиентов с указанием даты, начиная с которой они стали Клиентами: список отсортирован по году, месяцу, оборотам клиента по убыванию, имени Клиента.")]
        public void Linq005()
        {
            var customers =
                dataSource.Customers.OrderBy(c => c.Orders.OrderBy(o => o.OrderDate.Year).FirstOrDefault()?.OrderDate.Year)
                    .ThenBy(c => c.Orders.OrderBy(o => o.OrderDate.Year)
                    .ThenBy(o => o.OrderDate.Month).FirstOrDefault()?.OrderDate.Month)
                    .ThenByDescending(c => c.Orders.Sum(o => o.Total))
                    .ThenBy(c => c.CustomerID);

            foreach (var customer in customers)
            {
                var year = customer.Orders.OrderBy(o => o.OrderDate.Year).FirstOrDefault()?.OrderDate.Year;
                var month = customer.Orders.OrderBy(o => o.OrderDate.Year).FirstOrDefault()?.OrderDate.Month;
                var total = customer.Orders.Sum(o => o.Total);
                var customerId = customer.CustomerID;

                if (year != null && month != null)
                {
                    Console.WriteLine($"{year} - {month} - {total} - {customerId}");
                }
            }
        }

        [Category("Group")]
        [Title("LINQ-007")]
        [Description("Сгруппировать все продукты по категориям, внутри – по наличию на складе, внутри последней группы отсортируйте по стоимости.")]
        public void Linq7()
        {
            var result = dataSource.Products.Select(p => new { p.Category, p.UnitsInStock, p.ProductName, Cost = p.UnitPrice * p.UnitsInStock })
                                            .OrderBy(e => e.Cost)
                                            .GroupBy(e => new { e.Category, e.UnitsInStock })
                                            .OrderBy(g => g.Key.Category)
                                            .ThenBy(g => g.Key.UnitsInStock)
                                            .GroupBy(g => g.Key.Category);

            foreach (var categoryGroup in result)
            {
                Console.WriteLine($"Категория = {categoryGroup.Key}");
                foreach (var unitsInStockGroup in categoryGroup)
                {
                    Console.WriteLine($"В наличии = {unitsInStockGroup.Key.UnitsInStock}");
                    foreach (var element in unitsInStockGroup)
                    {
                        Console.WriteLine($"Продукт = {element.ProductName}, Стоимость = {element.Cost}");
                    }
                }
            }
        }

        [Category("Group")]
        [Title("LINQ-008")]
        [Description("Сгруппировать товары по группам «дешевые», «средняя цена», «дорогие». Границы каждой группы задайте сами")]
        public void Linq008()
        {
            var result = dataSource.Products.GroupBy(p =>
            {
                if (p.UnitPrice < 10)
                {
                    return "Дешевые";
                }

                return p.UnitPrice < 20 ? "Средняя цена" : "Дорогие";
            });

            foreach (var item in result)
            {
                foreach (var product in item)
                {
                    Console.WriteLine($"{product.ProductName} - {product.UnitPrice} - {item.Key}");
                }
            }
        }

        [Category("Statistic")]
        [Title("LINQ-009")]
        [Description("Рассчитать среднюю прибыльность каждого города (среднюю сумму заказа по всем клиентам из данного города) и среднюю интенсивность (среднее количество заказов, приходящееся на клиента из каждого города).")]
        public void Linq009()
        {
            var averages = dataSource.Customers.Select(c => c.City)
                                     .Distinct()
                                     .ToDictionary(city => city,
                                     city => dataSource.Customers.Where(c => c.City == city).SelectMany(c => c.Orders).Average(o => o.Total));

            Console.WriteLine("Средняя прибыльность города:");

            foreach (var average in averages)
            {
                Console.WriteLine($"{average.Key} - {average.Value:C}");
            }

            Console.WriteLine();

            var intensities = dataSource.Customers.Select(c => c.City)
                                        .Distinct()
                                        .ToDictionary(city => city,
                                        city =>
                                        (float)dataSource.Customers.Where(c => c.City == city).SelectMany(c => c.Orders).Count() /
                                         dataSource.Customers.Count(c => c.City == city));

            Console.WriteLine("Средняя интенсивность заказов:");

            foreach (var intensity in intensities)
            {
                Console.WriteLine($"{intensity.Key} - {intensity.Value:F}");
            }
        }

        [Category("Statistic")]
        [Title("LINQ-010")]
        [Description("Сделать среднегодовую статистику активности клиентов по месяцам (без учета года), статистику по годам, по годам и месяцам (т.е. когда один месяц в разные годы имеет своё значение).")]
        public void Linq010()
        {
            var ordersPerMonths = dataSource.Customers.SelectMany(c => c.Orders)
                                            .Select(o => o.OrderDate.Month)
                                            .Distinct()
                                            .ToDictionary(month => month,
                                                          month => dataSource.Customers.SelectMany(c => c.Orders).Count(o => o.OrderDate.Month == month))
                                            .OrderBy(o => o.Key);

            Console.WriteLine("Количество заказов в месяц:");

            foreach (var ordersPerMonth in ordersPerMonths)
            {
                Console.WriteLine($"Месяц: {ordersPerMonth.Key}, Количество заказов: {ordersPerMonth.Value}");
            }

            Console.WriteLine();

            var ordersPerYears = dataSource.Customers.SelectMany(c => c.Orders)
                                                     .Select(o => o.OrderDate.Year)
                                                     .Distinct()
                                                     .ToDictionary(year => year,
                                                                   year => dataSource.Customers.SelectMany(c => c.Orders).Count(o => o.OrderDate.Year == year))
                                                     .OrderBy(o => o.Key);

            Console.WriteLine("Количество заказов в год:");

            foreach (var ordersPerYear in ordersPerYears)
            {
                Console.WriteLine($"Год: {ordersPerYear.Key}, Количество заказов: {ordersPerYear.Value}");
            }

            Console.WriteLine();

            var ordersPerMonthsPerYears = dataSource.Customers.SelectMany(c => c.Orders)
                                                    .Select(o => o.OrderDate.Year)
                                                    .Distinct()
                                                    .ToDictionary(year => year,
                                                                  year =>
                                                                    dataSource.Customers.SelectMany(c => c.Orders)
                                                                        .Where(o => o.OrderDate.Year == year)
                                                                        .Select(o => o.OrderDate.Month)
                                                                        .Distinct()
                                                                        .ToDictionary(month => month,
                                                                            month =>
                                                                                dataSource.Customers.SelectMany(c => c.Orders)
                                                                                    .Count(o => o.OrderDate.Year == year && o.OrderDate.Month == month))
                                                                        .OrderBy(o => o.Key))
                                                    .OrderBy(o => o.Key);

            Console.WriteLine("Количество заказов по годам и месяцам в годе:");

            foreach (var ordersPerMonthsPerYear in ordersPerMonthsPerYears)
            {
                Console.WriteLine($"Год: {ordersPerMonthsPerYear.Key}");

                foreach (var ordersPerMonthPerYear in ordersPerMonthsPerYear.Value)
                {
                    Console.WriteLine($"Месяц: {ordersPerMonthPerYear.Key}, Количество заказов: {ordersPerMonthPerYear.Value}");
                }
            }
        }

        private void ShowResultByCustomerWithTurnover(int turnover)
        {
            var customers = GetCustomersWithTurnover(turnover);

            Console.WriteLine($"Клиенты c суммарным оборотом > {turnover}");

            foreach (var customer in customers)
            {
                Console.WriteLine($"ID Клиента: {customer.CustomerID}; Оборот: {customer.Orders.Select(o => o.Total).Sum()}");
            }
        }

        private IEnumerable<Customer> GetCustomersWithTurnover(int turnover)
        {
            return dataSource.Customers.Where(c => c.Orders.Select(o => o.Total).Sum() > turnover);
        }
    }
}
