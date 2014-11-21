COPYRIGHT = "Copyright 2012-2013 Chris Patterson, Albert Hives."

require File.dirname(__FILE__) + "/build_support/BuildUtils.rb"
require File.dirname(__FILE__) + "/build_support/util.rb"
include FileTest
require 'albacore'
require File.dirname(__FILE__) + "/build_support/versioning.rb"

PRODUCT = 'MassTransit-Quartz'
CLR_TOOLS_VERSION = 'v4.0.30319'
OUTPUT_PATH = 'bin/Release'

props = {
  :src => File.expand_path("src"),
  :nuget => File.join(File.expand_path("src"), ".nuget", "nuget.exe"),
  :output => File.expand_path("build_output"),
  :artifacts => File.expand_path("build_artifacts"),
  :lib => File.expand_path("lib"),
  :projects => ["MassTransit-Quartz"],
  :keyfile => File.expand_path("MassTransit.snk")
}

desc "Cleans, compiles, il-merges, unit tests, prepares examples, packages zip"
task :all => [:default, :package]

desc "**Default**, compiles and runs tests"
task :default => [:clean, :nuget_restore, :compile, :package]

desc "Update the common version information for the build. You can call this task without building."
assemblyinfo :global_version do |asm|
  # Assembly file config
  asm.product_name = PRODUCT
  asm.description = "MassTransit-Quartz provides message scheduling for MassTransit services."
  asm.version = FORMAL_VERSION
  asm.file_version = FORMAL_VERSION
  asm.custom_attributes :AssemblyInformationalVersion => "#{BUILD_VERSION}",
	:ComVisibleAttribute => false,
	:CLSCompliantAttribute => true
  asm.copyright = COPYRIGHT
  asm.output_file = 'src/SolutionVersion.cs'
  asm.namespaces "System", "System.Reflection", "System.Runtime.InteropServices"
end

desc "Prepares the working directory for a new build"
task :clean do
	FileUtils.rm_rf props[:output]
	waitfor { !exists?(props[:output]) }

	FileUtils.rm_rf props[:artifacts]
	waitfor { !exists?(props[:artifacts]) }

	Dir.mkdir props[:output]
	Dir.mkdir props[:artifacts]
end

desc "Cleans, versions, compiles the application and generates build_output/."
task :compile => [:versioning, :global_version, :build4, :tests4, :copy4]

task :copy35 => [:build35] do
  copyOutputFiles File.join(props[:src], "MassTransit.Scheduling/bin/Release/v3.5"), "MassTransit.Scheduling.{dll,pdb,xml}", File.join(props[:output], 'Scheduling', 'net-3.5')
end

task :copy4 => [:build4] do
  copyOutputFiles File.join(props[:src], "MassTransit.Scheduling/bin/Release"), "MassTransit.Scheduling.{dll,pdb,xml}", File.join(props[:output], 'Scheduling', 'net-4.0-full')
  copyOutputFiles File.join(props[:src], "MassTransit.QuartzIntegration/bin/Release"), "MassTransit.QuartzIntegration.{dll,pdb,xml}", File.join(props[:output], 'Integration', 'net-4.0-full')

  copyOutputFiles File.join(props[:src], "MassTransit.QuartzService/bin/Release"), "MassTransit.QuartzService.exe", File.join(props[:output], 'Service')
  copyOutputFiles File.join(props[:src], "MassTransit.QuartzService/bin/Release"), "*.dll", File.join(props[:output], 'Service')
  copyOutputFiles File.join(props[:src], "MassTransit.QuartzService/bin/Release"), "*.config", File.join(props[:output], 'Service')
end

desc "Only compiles the application."
msbuild :build4 do |msb|
	msb.properties :Configuration => "Release",
		:Platform => 'Any CPU'
	msb.use :net4
	msb.targets :Rebuild
  msb.properties[:SignAssembly] = 'true'
  msb.properties[:AssemblyOriginatorKeyFile] = props[:keyfile]
	msb.solution = 'src/MassTransit.Quartz.sln'
end

def copyOutputFiles(fromDir, filePattern, outDir)
	FileUtils.mkdir_p outDir unless exists?(outDir)
	Dir.glob(File.join(fromDir, filePattern)){|file|
		copy(file, outDir) if File.file?(file)
	}
end

desc "Runs unit tests"
nunit :tests4 => [:build4] do |nunit|
          nunit.command = File.join('src', 'packages','NUnit.Runners.2.6.3', 'tools', 'nunit-console.exe')
          nunit.parameters = "/framework=#{CLR_TOOLS_VERSION}", '/nothread', '/nologo', '/labels', "\"/xml=#{File.join(props[:artifacts], 'nunit-test-results-net-4.0.xml')}\""
          nunit.assemblies = FileList[File.join(props[:src], "MassTransit.QuartzIntegration.Tests/bin/Release", "MassTransit.QuartzIntegration.Tests.dll")]
end

task :package => [:nuget, :zip_output]

desc "ZIPs up the build results."
zip :zip_output => [:versioning] do |zip|
  zip.directories_to_zip = [props[:output]]
  zip.output_file = "MassTransit-Quartz-#{NUGET_VERSION}.zip"
  zip.output_path = props[:artifacts]
end

desc "restores missing packages"
msbuild :nuget_restore do |msb|
  msb.properties :SolutionDir => "../"
  msb.use :net4
  msb.targets :RestorePackages
  msb.solution = File.join(props[:src], "MassTransit.Scheduling", "MassTransit.Scheduling.csproj")
end

desc "Builds the nuget package"
task :nuget => [:versioning, :create_nuspec] do
  sh "#{props[:nuget]} pack #{props[:artifacts]}/MassTransit.Scheduling.nuspec /Symbols /OutputDirectory #{props[:artifacts]}"
  sh "#{props[:nuget]} pack #{props[:artifacts]}/MassTransit.QuartzIntegration.nuspec /Symbols /OutputDirectory #{props[:artifacts]}"
end

nuspec :create_nuspec do |nuspec|
  nuspec.id = 'MassTransit.Scheduling'
  nuspec.version = NUGET_VERSION
  nuspec.authors = 'Chris Patterson, Albert Hives'
  nuspec.summary = 'Scheduled messaging for MassTransit'
  nuspec.description = 'MassTransit Scheduling is used to schedule future message delivery'
  nuspec.title = 'MassTransit.Scheduling'
  nuspec.projectUrl = 'http://github.com/MassTransit/MassTransit-Quartz'
  nuspec.iconUrl = 'http://MassTransit-project.com/wp-content/themes/pandora/slide.1.png'
  nuspec.language = "en-US"
  nuspec.licenseUrl = "http://www.apache.org/licenses/LICENSE-2.0"
  nuspec.requireLicenseAcceptance = "false"
  nuspec.dependency "Magnum", "2.1.3"
  nuspec.dependency "MassTransit", "2.9.9"
  nuspec.output_file = File.join(props[:artifacts], 'MassTransit.Scheduling.nuspec')
  add_files File.join(props[:output], 'Scheduling'), 'MassTransit.Scheduling.{dll,pdb,xml}', nuspec
  nuspec.file(File.join(props[:src], "MassTransit.Scheduling\\**\\*.cs").gsub("/","\\"), "src")
end

nuspec :create_nuspec do |nuspec|
  nuspec.id = 'MassTransit.QuartzIntegration'
  nuspec.version = NUGET_VERSION
  nuspec.authors = 'Chris Patterson, Albert Hives'
  nuspec.summary = 'Quartz integration for MassTransit'
  nuspec.description = 'Adds support for Quartz as a message scheduler to MassTransit (used by the MassTransit.QuartzService project)'
  nuspec.title = 'MassTransit.QuartzIntegration'
  nuspec.projectUrl = 'http://github.com/MassTransit/MassTransit-Quartz'
  nuspec.iconUrl = 'http://MassTransit-project.com/wp-content/themes/pandora/slide.1.png'
  nuspec.language = "en-US"
  nuspec.licenseUrl = "http://www.apache.org/licenses/LICENSE-2.0"
  nuspec.requireLicenseAcceptance = "false"
  nuspec.dependency "Magnum", "2.1.3"
  nuspec.dependency "MassTransit", "2.9.9"
  nuspec.dependency "MassTransit.Scheduling", NUGET_VERSION
  nuspec.dependency "Common.Logging", "2.3.1"
  nuspec.dependency "Newtonsoft.Json", "6.0.6"
  nuspec.dependency "Quartz", "2.3.0"
  nuspec.output_file = File.join(props[:artifacts], 'MassTransit.QuartzIntegration.nuspec')
  add_files File.join(props[:output], 'Integration'), 'MassTransit.QuartzIntegration.{dll,pdb,xml}', nuspec
  nuspec.file(File.join(props[:src], "MassTransit.QuartzIntegration\\**\\*.cs").gsub("/","\\"), "src")
end

def project_outputs(props)
	props[:projects].map{ |p| "src/#{p}/bin/#{BUILD_CONFIG}/#{p}.dll" }.
		concat( props[:projects].map{ |p| "src/#{p}/bin/#{BUILD_CONFIG}/#{p}.exe" } ).
		find_all{ |path| exists?(path) }
end

def get_commit_hash_and_date
	begin
		commit = `git log -1 --pretty=format:%H`
		git_date = `git log -1 --date=iso --pretty=format:%ad`
		commit_date = DateTime.parse( git_date ).strftime("%Y-%m-%d %H%M%S")
	rescue
		commit = "git unavailable"
	end

	[commit, commit_date]
end

def add_files stage, what_dlls, nuspec
  [['net35', 'net-3.5'], ['net40', 'net-4.0'], ['net40-full', 'net-4.0-full']].each{|fw|
    takeFrom = File.join(stage, fw[1], what_dlls)
    Dir.glob(takeFrom).each do |f|
      nuspec.file(f.gsub("/", "\\"), "lib\\#{fw[0]}")
    end
  }
end

def waitfor(&block)
	checks = 0

	until block.call || checks >10
		sleep 0.5
		checks += 1
	end

	raise 'Waitfor timeout expired. Make sure that you aren\'t running something from the build output folders, or that you have browsed to it through Explorer.' if checks > 10
end
