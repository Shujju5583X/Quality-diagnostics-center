import sys

file_path = "LabSystem.UI/Views/DashboardView.xaml"

with open(file_path, "r", encoding="utf-8") as f:
    lines = f.readlines()

new_tab3 = """            <!-- ─── TAB 3: WORK QUEUE (UNIFIED MASTER-DETAIL) ─── -->
            <TabItem DataContext="{Binding UnifiedQueueVM}">
                <Grid Margin="12">
                    <Grid.ColumnDefinitions>
                        <!-- Master: Unified Queue -->
                        <ColumnDefinition Width="400" />
                        <!-- Detail: Dynamic Action Area -->
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>

                    <!-- MASTER: UNIFIED QUEUE -->
                    <materialDesign:Card Grid.Column="0" Margin="0 0 8 0" Padding="0" UniformCornerRadius="8" materialDesign:ShadowAssist.ShadowDepth="Depth2">
                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto"/>
                                <RowDefinition Height="*"/>
                            </Grid.RowDefinitions>
                            
                            <Border Grid.Row="0" Background="#311B92" CornerRadius="8 8 0 0" Padding="16 12">
                                <TextBlock Text="UNIFIED WORK QUEUE" FontWeight="Bold" FontSize="13" Foreground="White"/>
                            </Border>

                            <DataGrid Grid.Row="1" ItemsSource="{Binding QueueItems}" SelectedItem="{Binding SelectedItem}"
                                      AutoGenerateColumns="False" IsReadOnly="True" SelectionMode="Single"
                                      BorderThickness="0" Background="Transparent" AlternatingRowBackground="#FAF9FC"
                                      CanUserAddRows="False" CanUserDeleteRows="False" CanUserResizeRows="False">
                                <DataGrid.RowStyle>
                                    <Style TargetType="DataGridRow">
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding WorkflowState}" Value="AwaitingResults_Unpaid">
                                                <Setter Property="Background" Value="#FFF3E0"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding WorkflowState}" Value="ResultsReady_Unpaid">
                                                <Setter Property="Background" Value="#E3F2FD"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding WorkflowState}" Value="Completed_Paid">
                                                <Setter Property="Background" Value="#E8F5E9"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </DataGrid.RowStyle>
                                <DataGrid.Columns>
                                    <DataGridTextColumn Header="Order ID" Binding="{Binding OrderId}" Width="60" />
                                    <DataGridTextColumn Header="Patient" Binding="{Binding PatientName}" Width="*" />
                                    <DataGridTextColumn Header="State" Binding="{Binding WorkflowState}" Width="140" />
                                </DataGrid.Columns>
                            </DataGrid>
                        </Grid>
                    </materialDesign:Card>

                    <!-- DETAIL: DYNAMIC ACTION AREA -->
                    <materialDesign:Card Grid.Column="1" Margin="8 0 0 0" Padding="0" UniformCornerRadius="8" materialDesign:ShadowAssist.ShadowDepth="Depth2">
                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto"/>
                                <RowDefinition Height="*"/>
                                <RowDefinition Height="Auto"/>
                            </Grid.RowDefinitions>
                            
                            <Border Grid.Row="0" Background="#1565C0" CornerRadius="8 8 0 0" Padding="16 12">
                                <TextBlock Text="ACTION CENTER" FontWeight="Bold" FontSize="13" Foreground="White"/>
                            </Border>

                            <!-- Placeholder for selected item info -->
                            <StackPanel Grid.Row="1" Margin="16">
                                <TextBlock Text="{Binding SelectedItem.PatientName, StringFormat='Selected Patient: {0}', TargetNullValue='Select an order'}" FontSize="18" FontWeight="Bold"/>
                                <TextBlock Text="{Binding SelectedItem.WorkflowState, StringFormat='Current State: {0}'}" FontSize="14" Foreground="#666" Margin="0 4 0 16"/>

                                <!-- Quick Finalize Action Area -->
                                <Border Background="#F5F5F5" CornerRadius="8" Padding="16">
                                    <StackPanel>
                                        <TextBlock Text="Quick Finalize Action" FontWeight="Bold" FontSize="16" Margin="0 0 0 12"/>
                                        <TextBlock Text="This action will sequentially save all pending results, mark the invoice as fully paid (Cash), and trigger PDF document generation in a single database transaction." TextWrapping="Wrap" Foreground="#555" Margin="0 0 0 16"/>
                                        
                                        <Button Command="{Binding QuickFinalizeCommand}" Height="48" Style="{StaticResource MaterialDesignRaisedButton}">
                                            <StackPanel Orientation="Horizontal">
                                                <materialDesign:PackIcon Kind="Flash" Margin="0 0 8 0" VerticalAlignment="Center"/>
                                                <TextBlock Text="QUICK FINALIZE WORKFLOW" VerticalAlignment="Center" FontWeight="Bold"/>
                                            </StackPanel>
                                        </Button>
                                    </StackPanel>
                                </Border>
                            </StackPanel>
                        </Grid>
                    </materialDesign:Card>
                </Grid>
            </TabItem>
"""

# Replace lines 495 to 934 (0-indexed 495:935)
lines[495:935] = [new_tab3]

with open(file_path, "w", encoding="utf-8") as f:
    f.writelines(lines)

print("DashboardView.xaml updated successfully.")
