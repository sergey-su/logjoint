## Downloads
Web installers: [win](https://publogjoint.blob.core.windows.net/updates/logjoint.web.installer.exe), [mac](https://publogjoint.blob.core.windows.net/updates/logjoint-web-installer.dmg).

## Description
![Overview](https://github.com/sergey-su/logjoint/blob/master/doc/overview.png)

LogJoint is a log viewer and visualizer tool. It’s designed to simplify analysis of logs from multi-component multi-threaded systems.

High-level features:
- Reach [log viewing](#log-viewing) and navigation functionality.
- Log data can be retrieved from different textual or non-textual [sources](#log-sources).
- Multiple logs can be dynamically [joined](#joining-logs) into single flat view.
- Extensibility through domain-specific [plugins](#plugins) enable advanced visualizations and custom log retrieval methods.

## Log viewing
Reach log viewing functionality is made possible by lightweight on-the-fly log parsing that extracts basic information for each log message: timestamp, thread, severity, text message. This information enables features that plain-text tools can not offer.

Features:
- Search features allow you to find next/previous match as well as all occurrences. Matching can be done by text, severity or threads criteria. Additionally:
  - Multiple search results are combined on one view.
  - User can define and save named sets of favorite filtering rules.
  - Previous searches are saved in the history list.
- Bookmarks you set in a log are saved and are available next time you open the log. All current bookmarks are listed on separate view. [Screenshot](https://github.com/sergey-su/logjoint/blob/master/doc/bookmarks.png).
- Support of huge logs by loading only small fixed part at a time.
- Logs history list helps you recall recently open logs.
- Open logs’ time ranges and gaps are visualized. [Screenshot](https://github.com/sergey-su/logjoint/blob/master/doc/main_timeline.png).
- Messages from different threads and log sources have different background colors.
- With highlighting rules you can colorize the log custom way.
- Multiple views have *current time indicator* that help you understand relative position of currently selected logline. [Screenshot](https://github.com/sergey-su/logjoint/blob/master/doc/time_indicator.png).

## Log sources
Typically logs are stored as text files. LogJoint is not limited to any specific log file format. Instead, it can be taught to parse any format by defining your format in a config file. LogJoint ships with the set of configs for several common formats. LogJoint has wizards that generate configs automatically (currently supported: log4net and NLog pattern layout string import). Finally, you can use custom format wizard that will guide you through the process of creation the config file for your custom log format.
Some logs are not stored in text files. Viewing these need programmatic support, embedded or provided by a plugin. Out-of-box the following non-textual sources are supported:
- (win only) LogJoint can listen to [OutputDebugString()](https://msdn.microsoft.com/en-us/library/windows/desktop/aa363362(v=vs.85).aspx)
- (win only) Windows Event Log
- (win only) Azure Diagnostics Log

Additionally:
- The tool supports adding new log sources by drag&drop from Finder/Windows Explorer or from a web browser
- Archives are extracted on the fly
- Dragging an URL makes the tool download the linked contents. If required by the host, authentication information is taken from user and cached securely.
- Text log can have any encoding, including multibyte ones such as UTF8.

## Joining logs
When you open multiple logs at same time in the same LogJoint process the logs are dynamically merged into flat list ordered by timestamps. This is useful for tracing transactions that span multiple log files. If logs are from different machines and their clocks are out of sync you can manually set time shifts for individual logs.

Another feature is the option to monitor a folder for parts of rotated log. The parts discovered are dynamically joined and represented as single logical list.

## Plugins
Plugins extend different parts of LogJoint.
- Domain-specific plugin can define the way to extract visualizable information from certain types of logs. Visualizations are:
  - Timeline - displays activities such as network requests, lengthy procedures, lifespans on a Gantt chart.
  - Time series - depicts the change of numeric metrics over time
  - StateInspector - displays the state of logged objects at certain moment in time.
  - Sequence Diagram - useful for visualization of network messages.
- Plugin can implement custom log extraction methods. For example Azure plugin reads logs from Azure Storage.

## WebRTC plugin
WebRTC plugin lets LogJoint view and visualize Chrome Debug Log (https://webrtc.org/web-apis/chrome/) and webrtc_internals_dump.txt (produced by chrome://webrtc-internals/).

![Web RTC timeseries](https://github.com/sergey-su/logjoint/blob/master/doc/timeseries.png)
![WebRTC state Inspector](https://github.com/sergey-su/logjoint/blob/master/doc/state_inspector.png)