// Generated from CyberClubModel.edmx by CyberClubModel.tt.
using System;
using System.Collections.Generic;
namespace CyberClub.Models {
 public partial class User {
  public int Id {get;set;}
  public string Login {get;set;}
  public string PasswordHash {get;set;}
  public string FullName {get;set;}
  public string Role {get;set;}
  public string Photo {get;set;}
 }
 public partial class Client {
  public Client() {
   Bookings=new HashSet<Booking>();
  }
  public int Id {get;set;}
  public string FullName {get;set;}
  public string Phone {get;set;}
  public string Email {get;set;}
  public virtual ICollection<Booking> Bookings {get;set;}
 }
 public partial class Club {
  public Club() {
   Zones=new HashSet<Zone>();
  }
  public int Id {get;set;}
  public string Name {get;set;}
  public string Address {get;set;}
  public string Phone {get;set;}
  public virtual ICollection<Zone> Zones {get;set;}
 }
 public partial class Zone {
  public Zone() {
   Computers=new HashSet<Computer>();
  }
  public int Id {get;set;}
  public int ClubId {get;set;}
  public string Name {get;set;}
  public virtual Club Club {get;set;}
  public virtual ICollection<Computer> Computers {get;set;}
 }
 public partial class Computer {
  public Computer() {
   Bookings=new HashSet<Booking>();
  }
  public int Id {get;set;}
  public int ZoneId {get;set;}
  public string Number {get;set;}
  public string Specifications {get;set;}
  public string Status {get;set;}
  public virtual Zone Zone {get;set;}
  public virtual ICollection<Booking> Bookings {get;set;}
 }
 public partial class Tariff {
  public Tariff() {
   Bookings=new HashSet<Booking>();
  }
  public int Id {get;set;}
  public string Name {get;set;}
  public decimal HourlyRate {get;set;}
  public virtual ICollection<Booking> Bookings {get;set;}
 }
 public partial class Booking {
  public Booking() {
   Payments=new HashSet<Payment>();
  }
  public int Id {get;set;}
  public int ClientId {get;set;}
  public int ComputerId {get;set;}
  public int TariffId {get;set;}
  public DateTime StartsAt {get;set;}
  public DateTime EndsAt {get;set;}
  public decimal HourlyRate {get;set;}
  public decimal Total {get;set;}
  public string Status {get;set;}
  public virtual Client Client {get;set;}
  public virtual Computer Computer {get;set;}
  public virtual Tariff Tariff {get;set;}
  public virtual ICollection<Payment> Payments {get;set;}
 }
 public partial class Payment {
  public int Id {get;set;}
  public int BookingId {get;set;}
  public DateTime PaidAt {get;set;}
  public decimal Amount {get;set;}
  public string Method {get;set;}
  public virtual Booking Booking {get;set;}
 }
}
