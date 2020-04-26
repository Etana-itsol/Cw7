/*
select *
from Student
update Student set idenrollment=1

select* from Studies
select * from enrollment
update studies set name='IT'

alter procedure PromoteStudents @name nvarchar(50),@Semester int
as
begin
begin tran
	declare @IdStudies int=(select s.idStudy from Studies s join Enrollment e on e.IdStudy=s.IdStudy where name=@name and Semester=@Semester)
	if @IdStudies is null
	begin
		print'Niema takiego kierunku'
		rollback
		return
	end

	declare @oldEnrollment int =( select IdEnrollment from Studies s, Enrollment e where e.IdStudy= s.IdStudy and Semester = @semester and @Name = Name);
	declare @Id int=(select idenrollment from Enrollment e join Studies s on s.IdStudy=e.IdStudy where Semester=@Semester+1 and name =@name)
	if @Id is null
	begin
		set @Id= (select MAX(IdEnrollment)+1 from Enrollment);
		insert into Enrollment values(@id,@Semester+1,@IdStudies,GETDATE())
		print'Dodano Enrollment'
	end

	update student set IdEnrollment = @Id where IdEnrollment=@oldEnrollment;
	print'promocja studentow'
	commit
end


Execute PromoteStudents 'IT', 1;


alter procedure EnrollStudent  @indexNumber nvarchar(30), @FirstName nvarchar(15), @LastName nvarchar(60),@BirthDate Date,@Studies nvarchar(100)
as
begin
	begin tran

	declare @IdStudies int = (select IdStudy from studies where name=@Studies);
	declare @NumberExist int = (select count(*) from Student where IndexNumber=@indexNumber);
	if @NumberExist != 0
	begin
		print 'Student o takim ID juz istnieje';
		rollback;
		return;
	end;
	if @IdStudies is null
	begin
		print 'Nie ma takich studiow';
		rollback;
		return;
	end;
	declare @IdEnrollment int = (select top 1 IdEnrollment
								from Enrollment where Semester=1 and IdStudy=@IdStudies
								order by StartDate desc);
	if @IdEnrollment is null
	begin
		declare @currentDate Date = (SELECT CONVERT(char(10), GetDate(),126));
		declare @maxIdEnrollment int = (select max(IdEnrollment) from Enrollment);

		insert into Enrollment (IdEnrollment, Semester,IdStudy,StartDate)
		values ((@maxIdEnrollment+1), 1, @IdStudies, @currentDate);

		Print 'Dodano element';
		set @IdEnrollment = @maxIdEnrollment +1;
	end;
	insert into Student (IndexNumber,FirstName,LastName,BirthDate,IdEnrollment) 
	values (@indexNumber, @FirstName, @LastName, @BirthDate, @IdEnrollment);
	commit;
end;

		
exec EnrollStudent  '12334', 'Mateusz', 'Pawlak', '1998-03-03','IT'
*/
